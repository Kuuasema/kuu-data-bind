using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class AddXPButton : MonoBehaviour {
    
    private Button button;

    private void Awake() {
        this.button = this.GetComponent<Button>();
        this.button.onClick.AddListener(this.OnClick);
    }

    private void OnClick() {
        Demo.AddXP(10);
    }
}


