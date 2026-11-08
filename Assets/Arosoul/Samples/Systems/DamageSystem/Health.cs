using System;
using System.Collections;
using Alchemy.Inspector;
using UnityEngine;

public class Health : MonoBehaviour {
    [SerializeField] private int _startHealth = 3;

    [SerializeField, ReadOnly] private int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public void Damage(int amount) {
        _currentHealth -= amount;
        ValidateValues();
        OnHealthChanged?.Invoke(this, -amount);
    }
    public void Heal(int amount) {
        _currentHealth += amount;
        ValidateValues();
        OnHealthChanged?.Invoke(this, amount);
    }

    [ContextMenu("Kill")]
    public void Kill() {
        int delta = -_currentHealth;
        _currentHealth = 0;
        ValidateValues();
        OnHealthChanged?.Invoke(this, delta);
    }
    
    public void SetHealthToMaxValue() {
        int delta = _maxHealth - _currentHealth;
        _currentHealth = _maxHealth;
        ValidateValues();
        OnHealthChanged?.Invoke(this, delta);
    }

    private int _maxHealth;
    public int MaxHealth => _maxHealth;


    public void SetMaxHealth(int value) {
        int delta = value - _maxHealth;
        _maxHealth = value;
        ValidateValues();
        OnMaxHealthChanged?.Invoke(this, delta);
    }

    

    public event EventHandler OnInitializedEndOfFrame;

    public event EventHandler<int> OnHealthChanged; // Int: Health delta
    public event EventHandler<int> OnMaxHealthChanged; // Int: Max health delta

    public event EventHandler OnHealthEmpty;
    public event EventHandler OnHealthFull;


    private void ValidateValues() {
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        _maxHealth = Mathf.Max(_maxHealth, 0);
        
        if (_currentHealth == 0) {
            OnHealthEmpty?.Invoke(this, EventArgs.Empty);
        }
        if (_currentHealth == _maxHealth) {
            OnHealthFull?.Invoke(this, EventArgs.Empty);
        }
    }

    void Start() {
        _maxHealth = _startHealth;
        _currentHealth = _startHealth;

        print(_currentHealth);

        // Call at end of frame
        StartCoroutine(EndOfFrameCoroutine(() => {
            OnInitializedEndOfFrame?.Invoke(this, EventArgs.Empty);
        }));
    }

    
    IEnumerator EndOfFrameCoroutine(Action action) {
        yield return new WaitForEndOfFrame();
        action?.Invoke();
    }

    
    
}