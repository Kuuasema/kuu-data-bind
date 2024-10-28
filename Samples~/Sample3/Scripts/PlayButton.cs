using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class PlayButton : ViewModel<float> {
    [ViewBind]
    private Button button;

    [SerializeField]
    private float cost;

    protected override void Awake() {
        base.Awake();
        this.button.onClick.AddListener(this.OnClick);
    }

    private float ActiveCost() {
        float cost = this.cost;
        if (Demo.DataModel.Settings.EnablePowerSave.Value) {
            cost /= 2.0f;
        }
        return cost;
    }

    protected override void OnValueUpdated(float value) {
        this.button.interactable = value >= this.ActiveCost();
    }

    private void OnClick() {
        this.DataModel.RemoveValue(this.ActiveCost());
        Demo.Play();
    }
}


