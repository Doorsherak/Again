// ResProbe.cs (�� ������Ʈ�� ����)
using UnityEngine;
using System.Collections; // �� �߰�

public class ResProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void Kick() { new GameObject("ResProbe").AddComponent<ResProbe>(); }

    IEnumerator Start()
    {
        // 1) ��� �� 1������ ��� �� �ػ� �� 0.1s �� ��ȣ�� (�ּ�����)
        var exclusive = true; // �׽�Ʈ�� ���� ���̴� Exclusive ����
        var mode = exclusive ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;

        Screen.fullScreenMode = mode;
        yield return new WaitForEndOfFrame();

#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(1280, 720, mode, Screen.currentResolution.refreshRateRatio);
#else
        Screen.SetResolution(1280, 720, mode, Screen.currentResolution.refreshRate);
#endif
        yield return new WaitForSecondsRealtime(0.1f);
#if UNITY_6000_0_OR_NEWER || UNITY_2022_2_OR_NEWER
        Screen.SetResolution(1920, 1080, mode, Screen.currentResolution.refreshRateRatio);
#else
        Screen.SetResolution(1920, 1080, mode, Screen.currentResolution.refreshRate);
#endif
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 60),
            $"RES: {Screen.width}��{Screen.height}\nMODE: {Screen.fullScreenMode}");
    }
}
