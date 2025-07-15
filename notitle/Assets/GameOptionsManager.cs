using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    private Resolution[] resolutions; // 'resolutions' 변수 선언 추가
    public Dropdown resolutionDropdown; // 'resolutionDropdown' 변수 선언 추가

    void Start()
    {
        InitializeOptions();
        LoadSettings();
    }

    void InitializeOptions()
    {
        // 해상도 설정
        resolutions = Screen.resolutions; // 'resolutions' 변수 사용
        resolutionDropdown.ClearOptions();
        List<string> resolutionOptions = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        Debug.Log("LoadSettings 메서드가 호출되었습니다.");
    }
}
