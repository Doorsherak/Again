using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;

public class GameOptionsManager : MonoBehaviour
{
    private Resolution[] resolutions; // 'resolutions' ���� ���� �߰�
    public Dropdown resolutionDropdown; // 'resolutionDropdown' ���� ���� �߰�

    void Start()
    {
        InitializeOptions();
        LoadSettings();
    }

    void InitializeOptions()
    {
        // �ػ� ����
        resolutions = Screen.resolutions; // 'resolutions' ���� ���
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
        Debug.Log("LoadSettings �޼��尡 ȣ��Ǿ����ϴ�.");
    }
}
