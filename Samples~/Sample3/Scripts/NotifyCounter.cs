using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Kuuasema.DataBinding;

public class NotifyCounter : ViewModel<int> {

    public string format;

    [ViewBind("Text_Value")]
    private TextMeshProUGUI text;
    
    protected override void SetupView() { 
        if (this.DataModel == null) {
            this.gameObject.SetActive(false);
            return;
        }
        
        this.OnValueUpdated(this.DataModel.Value);
    }

    protected override void OnValueUpdated(int value) {
        if (value < 1) {
            this.gameObject.SetActive(false);
            return;
        }

        this.gameObject.SetActive(true);
        this.text.text = string.Format(this.format, value);
    }
}
