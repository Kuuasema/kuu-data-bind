using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.Utils;
using Kuuasema.DataBinding;


public class SeasonView : SeasonViewModel {
    [SerializeField]
    private Season enableSeason;

    protected override void SetupView() { 
        if (this.DataModel == null) {
            this.gameObject.SetActive(false);
            return;
        }
        
        this.OnValueUpdated(this.DataModel.Value);
    }

    protected override void OnValueUpdated(Season value) {
        this.gameObject.TrySetActive(value == this.enableSeason);
    }
}
