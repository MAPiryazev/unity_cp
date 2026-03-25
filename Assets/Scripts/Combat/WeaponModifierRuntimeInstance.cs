using UnityEngine;

public sealed class WeaponModifierRuntimeInstance
{
    readonly WeaponModifierDefinition _definition;
    readonly StatWeaponModifierTemplate _template;
    float _expiresAt;

    public WeaponModifierRuntimeInstance(WeaponModifierDefinition definition, float duration)
    {
        _definition = definition;
        _template = default;
        TotalDuration = Mathf.Max(0.1f, duration);
        Refresh(TotalDuration);
    }

    public WeaponModifierRuntimeInstance(StatWeaponModifierTemplate template, float duration)
    {
        _definition = null;
        _template = template;
        TotalDuration = Mathf.Max(0.1f, duration);
        Refresh(TotalDuration);
    }

    public WeaponModifierDefinition Definition => _definition;
    public StatWeaponModifierTemplate Template => _template;
    public float TotalDuration { get; private set; }
    public float RemainingDuration => Mathf.Max(0f, _expiresAt - Time.time);
    public bool IsExpired => RemainingDuration <= 0.0001f;
    public string DisplayName => _definition != null ? _definition.DisplayName : _template.DisplayName;
    public string Description => _definition != null ? _definition.Description : _template.Description;

    public bool Matches(WeaponModifierDefinition definition)
    {
        return definition != null && _definition == definition;
    }

    public bool Matches(StatWeaponModifierTemplate template)
    {
        return _definition == null && _template.Equals(template);
    }

    public void Refresh(float duration)
    {
        TotalDuration = Mathf.Max(0.1f, duration);
        _expiresAt = Time.time + TotalDuration;
    }

    public void Apply(ref HitscanWeaponSettings settings)
    {
        if (_definition != null)
            _definition.Apply(ref settings);
        else
            _template.Apply(ref settings);
    }
}
