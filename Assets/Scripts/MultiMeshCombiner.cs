using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class MultiMeshCombiner : MonoBehaviour
{
        [ContextMenu("에디터에서 합치고 전용 폴더에 저장")]
        public void CombineAndSaveWithConfirm()
        {
#if UNITY_EDITOR
                string rootPath = "Assets/CombinedMeshes";
                string folderName = gameObject.name;
                string targetPath = Path.Combine(rootPath, folderName);

                // 1. 폴더 및 파일 정리 로직
                if (AssetDatabase.IsValidFolder(targetPath))
                {
                        bool confirm = EditorUtility.DisplayDialog("폴더 중복 확인",
                            $"'{folderName}' 폴더가 이미 존재합니다.\n기존 메쉬를 지우고 새로 합치시겠습니까?",
                            "덮어쓰기 (Yes)", "취소 (No)");

                        if (!confirm) return;

                        string[] files = Directory.GetFiles(targetPath);
                        foreach (string file in files) File.Delete(file);
                }
                else
                {
                        if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "CombinedMeshes");
                        AssetDatabase.CreateFolder(rootPath, folderName);
                }

                // 2. 스케일 보정 전 상태 기록 (Undo 가능하게)
                Undo.RegisterFullObjectHierarchyUndo(gameObject, "Combine Meshes");

                Vector3 originalScale = transform.localScale;
                transform.localScale = Vector3.one;

                Dictionary<Material, List<CombineInstance>> combiners = new Dictionary<Material, List<CombineInstance>>();

                // 3. 자식 메시 수집 (Combined_ 제외)
                MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
                foreach (var filter in meshFilters)
                {
                        if (filter.gameObject == gameObject || filter.name.StartsWith("Combined_")) continue;
                        if (filter.transform.parent != transform && filter.transform.parent.name.StartsWith("Combined_")) continue;

                        MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                        if (!renderer || !filter.sharedMesh) continue;

                        Material mat = renderer.sharedMaterial;
                        if (mat == null) continue;

                        if (!combiners.ContainsKey(mat)) combiners[mat] = new List<CombineInstance>();

                        CombineInstance ci = new CombineInstance();
                        ci.mesh = filter.sharedMesh;
                        ci.transform = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                        combiners[mat].Add(ci);

                        // 중요: 원본을 끄는 행위 기록
                        Undo.RecordObject(filter.gameObject, "Hide Mesh");
                        filter.gameObject.SetActive(false);
                }

                // 4. 새로운 메시 생성 및 오브젝트 배치
                foreach (Material mat in combiners.Keys)
                {
                        Mesh combinedMesh = new Mesh();
                        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        combinedMesh.CombineMeshes(combiners[mat].ToArray(), true, true);
                        combinedMesh.RecalculateBounds();
                        combinedMesh.name = mat.name + "_Combined";

                        string fileName = $"{mat.name}_Combined.asset";
                        string assetPath = Path.Combine(targetPath, fileName);
                        AssetDatabase.CreateAsset(combinedMesh, assetPath);

                        GameObject newObj = new GameObject("Combined_" + mat.name);
                        Undo.RegisterCreatedObjectUndo(newObj, "Create Combined Mesh");

                        newObj.transform.SetParent(transform);
                        newObj.transform.localPosition = Vector3.zero;
                        newObj.transform.localRotation = Quaternion.identity;
                        newObj.transform.localScale = Vector3.one;

                        newObj.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
                        newObj.AddComponent<MeshRenderer>().sharedMaterial = mat;
                }

                transform.localScale = originalScale;

                // 5. [핵심] 프리팹/씬 변경사항 강제 저장
                EditorUtility.SetDirty(gameObject);

                // 프리팹 모드인 경우 프리팹 자체를 저장
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ [{folderName}] 메시 합치기 및 저장 완료!");
#endif
        }

        [ContextMenu("합치기 해제 (원본 복구)")]
        public void Uncombine()
        {
#if UNITY_EDITOR
                Undo.RegisterFullObjectHierarchyUndo(gameObject, "Uncombine Meshes");

                // 1. Combined_ 오브젝트 삭제
                List<GameObject> toDelete = new List<GameObject>();
                foreach (Transform child in transform)
                {
                        if (child.name.StartsWith("Combined_")) toDelete.Add(child.gameObject);
                }

                foreach (GameObject obj in toDelete) Undo.DestroyObjectImmediate(obj);

                // 2. 모든 자식 중 꺼져있는 메시 복구
                MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
                int restoredCount = 0;
                foreach (MeshFilter filter in meshFilters)
                {
                        if (filter.gameObject == gameObject) continue;
                        if (filter.name.StartsWith("Combined_")) continue;

                        if (!filter.gameObject.activeSelf)
                        {
                                Undo.RecordObject(filter.gameObject, "Restore Mesh");
                                filter.gameObject.SetActive(true);
                                restoredCount++;
                        }
                }

                // 3. 변경사항 저장
                EditorUtility.SetDirty(gameObject);
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null) EditorSceneManager.MarkSceneDirty(prefabStage.scene);

                AssetDatabase.SaveAssets();
                Debug.Log($"✅ 합본 {toDelete.Count}개 삭제, 원본 {restoredCount}개 복구 완료!");
#endif
        }
}