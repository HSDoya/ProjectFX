using UnityEditor;
using UnityEngine;

// 세이브 기능을 손으로 확인하기 위한 에디터 메뉴.
//
// 게임 안의 저장/불러오기 UI는 Phase 5에서 MenuWindow에 탭으로 붙일 예정이다.
// 그 전까지 동작을 확인할 수단이 없어서 임시로 둔다. Editor 폴더에 있으므로 빌드에는 포함되지 않는다.
//
// 플레이 모드에서만 쓸 수 있다. 저장할 값을 들고 있는 것은 씬에 떠 있는 런타임 오브젝트들이라
// 에디트 모드에서 누르면 전부 비어 있는 세이브가 만들어진다.
public static class SaveMenu
{
    private const string Root = "Tools/세이브/";

    [MenuItem(Root + "저장 (슬롯 0)", false, 1)]
    private static void Save()
    {
        if (!RequirePlayMode()) return;

        if (SaveManager.Save(0))
            UnityEngine.Debug.Log($"<color=green>저장 완료</color> - {SaveManager.PathFor(0)}");
    }

    [MenuItem(Root + "불러오기 (슬롯 0)", false, 2)]
    private static void Load()
    {
        if (!RequirePlayMode()) return;

        if (!SaveManager.Exists(0))
        {
            EditorUtility.DisplayDialog("세이브", "슬롯 0에 저장된 파일이 없습니다.\n먼저 저장해 주세요.", "확인");
            return;
        }

        if (SaveManager.Load(0))
            UnityEngine.Debug.Log("<color=green>불러오기 완료</color>");
    }

    [MenuItem(Root + "슬롯 0 삭제", false, 20)]
    private static void Delete()
    {
        if (!SaveManager.Exists(0))
        {
            EditorUtility.DisplayDialog("세이브", "지울 파일이 없습니다.", "확인");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "세이브 삭제",
            $"슬롯 0의 세이브를 지웁니다. 되돌릴 수 없습니다.\n\n{SaveManager.PathFor(0)}",
            "삭제", "취소");

        if (ok) SaveManager.Delete(0);
    }

    [MenuItem(Root + "세이브 폴더 열기", false, 40)]
    private static void OpenFolder()
    {
        string folder = Application.persistentDataPath;
        System.IO.Directory.CreateDirectory(folder); // 아직 한 번도 저장 안 했으면 없을 수 있다
        EditorUtility.RevealInFinder(folder);
    }

    [MenuItem(Root + "현재 세이브 내용 출력", false, 41)]
    private static void Dump()
    {
        string path = SaveManager.PathFor(0);
        if (!System.IO.File.Exists(path))
        {
            UnityEngine.Debug.Log("[Save] 슬롯 0에 파일이 없습니다.");
            return;
        }

        UnityEngine.Debug.Log($"[Save] {path}\n{System.IO.File.ReadAllText(path)}");
    }

    private static bool RequirePlayMode()
    {
        if (EditorApplication.isPlaying) return true;

        EditorUtility.DisplayDialog(
            "세이브",
            "플레이 모드에서만 사용할 수 있습니다.\n\n저장할 값은 실행 중인 씬의 오브젝트가 들고 있습니다.",
            "확인");
        return false;
    }
}
