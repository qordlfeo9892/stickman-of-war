using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown fullscreenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
    };

    private const string VolumeKey = "MasterVolume";
    private const string FullscreenModeKey = "FullscreenModeIndex"; // 0 = 전체화면, 1 = 창모드
    private const string ResolutionIndexKey = "ResolutionIndex";
    private const int DefaultResolutionIndex = 2; // 1920x1080

    private void OnEnable()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.SetValueWithoutNotify(volume);
        AudioListener.volume = volume;

        int defaultModeIndex = Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0;
        int modeIndex = PlayerPrefs.GetInt(FullscreenModeKey, defaultModeIndex);
        fullscreenModeDropdown.SetValueWithoutNotify(modeIndex);

        int resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, FindClosestResolutionIndex());
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullscreenModeDropdown.onValueChanged.AddListener(OnDisplaySettingsChanged);
        resolutionDropdown.onValueChanged.AddListener(OnDisplaySettingsChanged);
    }

    private void OnDisable()
    {
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        fullscreenModeDropdown.onValueChanged.RemoveListener(OnDisplaySettingsChanged);
        resolutionDropdown.onValueChanged.RemoveListener(OnDisplaySettingsChanged);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void OnDisplaySettingsChanged(int _)
    {
        int modeIndex = fullscreenModeDropdown.value;
        int resolutionIndex = resolutionDropdown.value;
        Vector2Int resolution = Resolutions[resolutionIndex];
        FullScreenMode mode = modeIndex == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(resolution.x, resolution.y, mode);

        PlayerPrefs.SetInt(FullscreenModeKey, modeIndex);
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    private int FindClosestResolutionIndex()
    {
        int currentWidth = Screen.currentResolution.width;
        int bestIndex = DefaultResolutionIndex;
        int bestDiff = int.MaxValue;

        for (int i = 0; i < Resolutions.Length; i++)
        {
            int diff = Mathf.Abs(Resolutions[i].x - currentWidth);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
