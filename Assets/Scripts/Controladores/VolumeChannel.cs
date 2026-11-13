using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

// Une un Slider + Label con un parámetro expuesto del AudioMixer
public class VolumeChannel
{
    public readonly Slider Slider;
    public event System.Action<float> Changed;

    private readonly AudioMixer _mixer;
    private readonly string _param;
    private readonly string _prefsKey;
    private readonly Label _label;
    private readonly EventCallback<ChangeEvent<float>> _callback;

    public VolumeChannel(AudioMixer mixer, string param, Slider slider, Label label, float defaultValue)
    {
        _mixer = mixer;
        _param = param;
        _prefsKey = "vol_" + param;
        Slider = slider;
        _label = label;

        float saved = PlayerPrefs.GetFloat(_prefsKey, defaultValue);
        Slider.SetValueWithoutNotify(saved);
        UpdateLabel(saved);

        _callback = evt =>
        {
            Apply(evt.newValue);
            PlayerPrefs.SetFloat(_prefsKey, evt.newValue);
            Changed?.Invoke(evt.newValue);
        };
        Slider.RegisterValueChangedCallback(_callback);
    }

    public void ApplyCurrent() => Apply(Slider.value);

    private void Apply(float linear)
    {
        if (_mixer != null) _mixer.SetFloat(_param, LinearToDecibels(linear));
        UpdateLabel(linear);
    }

    private void UpdateLabel(float linear) =>
        _label.text = Mathf.RoundToInt(linear * 100f) + "%";

    public void Dispose() => Slider.UnregisterValueChangedCallback(_callback);

    private static float LinearToDecibels(float linear) =>
        linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
}