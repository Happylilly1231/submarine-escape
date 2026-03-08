using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
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

        // --- [추가] 덮어쓰기 확인 로직 ---
        if (AssetDatabase.IsValidFolder(targetPath))
        {
            bool confirm = EditorUtility.DisplayDialog("폴더 중복 확인",
                $"'{folderName}' 폴더가 이미 존재합니다.\n기존 메쉬를 지우고 새로 합치시겠습니까?",
                "덮어쓰기 (Yes)", "취소 (No)");

            if (!confirm)
            {
                Debug.Log("❌ 작업을 취소했습니다. 모델 이름을 바꾸고 다시 시도하세요.");
                return;
            }

            // 기존 폴더 안의 파일들 정리 (깔끔한 덮어쓰기를 위해)
            string[] files = Directory.GetFiles(targetPath);
            foreach (string file in files) File.Delete(file);
        }
        else
        {
            // 폴더가 없으면 생성
            if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "CombinedMeshes");
            AssetDatabase.CreateFolder(rootPath, folderName);
        }

        // --- 스케일 보정 ---
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.one;

        Dictionary<Material, List<CombineInstance>> combiners = new Dictionary<Material, List<CombineInstance>>();
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (var filter in meshFilters)
        {
            if (filter.gameObject == gameObject || filter.name.StartsWith("Combined_")) continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (!renderer || !filter.sharedMesh) continue;

            Material mat = renderer.sharedMaterial;
            if (!combiners.ContainsKey(mat)) combiners[mat] = new List<CombineInstance>();

            CombineInstance ci = new CombineInstance();
            ci.mesh = filter.sharedMesh;
            ci.transform = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            combiners[mat].Add(ci);

            filter.gameObject.SetActive(false);
        }

        foreach (Material mat in combiners.Keys)
        {
            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combiners[mat].ToArray(), true, true);
            combinedMesh.RecalculateBounds();

            // 덮어쓸 때는 타임스탬프 없이 깔끔하게 이름 지정
            string fileName = $"{mat.name}_Combined.asset";
            string assetPath = Path.Combine(targetPath, fileName);
            AssetDatabase.CreateAsset(combinedMesh, assetPath);

            GameObject newObj = new GameObject("Combined_" + mat.name);
            newObj.transform.parent = transform;
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;

            MeshFilter mf = newObj.AddComponent<MeshFilter>();
            mf.sharedMesh = combinedMesh;

            MeshRenderer mr = newObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        transform.localScale = originalScale;

        // Dirty 체크 및 저장
        EditorUtility.SetDirty(gameObject);
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ [{folderName}] 폴더에 메시가 성공적으로 저장되었습니다!");
#endif
    }
}