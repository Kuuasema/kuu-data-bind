using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;


public class SetSeasonButton : MonoBehaviour {

    [SerializeField]
    private Season season;
    
    private Button button;

    private void Awake() {
        this.button = this.GetComponent<Button>();
        this.button.onClick.AddListener(this.OnClick);
    }

    private void OnClick() {
        Demo.SetSeason(this.season);
    }
}


