using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
namespace Kuuasema.DataBinding {
    public class FormattedStringViewModel : ViewModel<string> {
        public string Format;
        [ViewBind]
        // [SerializeField]
        protected TextMeshProUGUI text;
        public TextMeshProUGUI Text => this.text;

        protected override void SetupView() { 
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }

        protected override void OnValueUpdated(string value) {
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }
    }
}
