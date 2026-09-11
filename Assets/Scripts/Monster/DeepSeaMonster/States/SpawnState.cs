using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace DeepSeaMonsterStates
{
    public class SpawnState : IState<DeepSeaMonsterController>
    {
        public void Enter(DeepSeaMonsterController owner)
        {
            // 1. 패턴 및 스폰 트랜스폼(위치, 회전) 결정
            if (TryDecidePatternAndSpawnTransform(owner, out DeepSeaMonsterAttackBase selectedPattern, out Vector3 spawnPos, out Quaternion spawnRot))
            {
                owner.currentPattern = selectedPattern;

                // 2. 괴물 실제 위치 & 회전 세팅 및 활성화
                owner.transform.position = spawnPos;
                owner.transform.rotation = spawnRot;
                owner.gameObject.SetActive(true);

                Debug.Log($"괴물 나타남 - 현재 패턴: {owner.currentPattern.GetType().Name} / 위치: {spawnPos}");

                // 3. 패턴 연출 초기화 (사운드/애니메이션 등)
                owner.currentPattern.OnSpawned();

                // 등장 소리 재생
                owner.audioSource.volume = 1f;
                owner.audioSource.PlayOneShot(owner.spawnSound);

                // 4. 이동 상태로 전환
                owner.ChangeState(new MoveState());
            }
            else
            {
                // 스폰 가능한 패턴/방향이 하나도 없으면 림보(HiddenState)로 복귀
                Debug.Log("스폰 가능한 위치가 없어 다시 숨음");
                owner.currentPattern = null;
                owner.ChangeState(new HiddenState());
            }
        }

        public void Update(DeepSeaMonsterController owner)
        {

        }

        public void Exit(DeepSeaMonsterController owner)
        {

        }

        /// <summary>
        /// 패턴 선택 및 해당 패턴의 최종 스폰 위치/회전을 함께 결정하는 함수
        /// </summary>
        private bool TryDecidePatternAndSpawnTransform(
            DeepSeaMonsterController monster,
            out DeepSeaMonsterAttackBase selectedPattern,
            out Vector3 spawnPos,
            out Quaternion spawnRot)
        {
            selectedPattern = null;
            spawnPos = Vector3.zero;
            spawnRot = Quaternion.identity;

            // <패턴, 가능했던 월드 방향 리스트> 저장용 딕셔너리
            var validPatternsMap = new Dictionary<DeepSeaMonsterAttackBase, List<Vector3>>();

            // 1. 전체 패턴 순회하며 스폰 가능한 방향 수집
            foreach (var pattern in monster.AllPatternList)
            {
                List<Vector3> validDirs = GetAvailableDirections(monster, pattern);
                if (validDirs.Count > 0)
                {
                    validPatternsMap.Add(pattern, validDirs);
                }
            }

            // 스폰 가능한 패턴이 하나도 없음
            if (validPatternsMap.Count == 0) return false;

            // 2. 등장 횟수가 가장 적은 패턴들 추출
            int minCount = validPatternsMap.Keys.Min(p => monster.GetPatternCount(p));
            List<DeepSeaMonsterAttackBase> candidates = validPatternsMap.Keys
                .Where(p => monster.GetPatternCount(p) == minCount)
                .ToList();

            // 3. 후보 패턴 중 무작위 1개 선택
            selectedPattern = candidates[Random.Range(0, candidates.Count)];
            monster.IncreasePatternCount(selectedPattern);

            // 4. 선택된 패턴이 가진 유효한 방향 중 무작위 1개 선택
            List<Vector3> availableWorldDirs = validPatternsMap[selectedPattern];
            Vector3 chosenWorldDir = availableWorldDirs[Random.Range(0, availableWorldDirs.Count)];

            // 5. 최종 스폰 좌표 및 바라보는 회전값 계산
            Vector3 playerPos = monster.PlayerTransform.position;
            spawnPos = playerPos + (chosenWorldDir * selectedPattern.SpawnDistance);

            // 괴물이 플레이어를 향하도록 회전값 설정
            Vector3 lookDirection = (playerPos - spawnPos).normalized;
            if (lookDirection != Vector3.zero)
            {
                spawnRot = Quaternion.LookRotation(lookDirection);
            }

            return true;
        }

        /// <summary>
        /// 해당 패턴의 스폰 방향 중 '현재 스폰 가능한 월드 방향들'을 구함
        /// </summary>
        private List<Vector3> GetAvailableDirections(DeepSeaMonsterController monster, DeepSeaMonsterAttackBase pattern)
        {
            List<Vector3> validWorldDirs = new List<Vector3>();
            Vector3 playerPos = monster.PlayerTransform.position;
            float distance = pattern.SpawnDistance;

            foreach (Vector3 localDir in pattern.SpawnDirections)
            {
                Vector3 worldDir = ConvertToWorldDirection(monster, localDir);

                if (CheckSpawnDirAvailable(monster, playerPos, worldDir, distance))
                {
                    validWorldDirs.Add(worldDir);
                }
            }

            return validWorldDirs;
        }

        private Vector3 ConvertToWorldDirection(DeepSeaMonsterController monster, Vector3 localDir)
        {
            if (localDir == Vector3.forward || localDir == Vector3.back)
            {
                return monster.PlayerTransform.TransformDirection(localDir);
            }

            return localDir;
        }

        private bool CheckSpawnDirAvailable(DeepSeaMonsterController monster, Vector3 playerPos, Vector3 worldDir, float distance)
        {
            Vector3 targetSpawnPos = playerPos + (worldDir * distance);

            // [1단계 CheckSphere] 스폰 지점 자체 공간 검사
            bool isSpawnPointBlocked = Physics.CheckSphere(
                targetSpawnPos,
                monster.MonsterRadius,
                monster.obstacleLayerMask
            );

            if (isSpawnPointBlocked) return false;

            // [2단계 SphereCast] 스폰 지점 -> 플레이어 방향 경로 검사
            Vector3 rayDirection = (playerPos - targetSpawnPos).normalized;

            bool isPathBlocked = Physics.SphereCast(
                targetSpawnPos,
                monster.MonsterRadius,
                rayDirection,
                out _,
                distance,
                monster.obstacleLayerMask
            );

            return !isPathBlocked;
        }
    }
}