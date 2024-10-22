
using UnityEngine;
using Kuuasema.DataBinding;
using TMPro;

namespace Kuuasema.Core {
    public class InputFieldTextViewModel : ViewModel<string> {
        [ViewBind]
        private TMP_InputField input;
        public TMP_InputField InputField => this.input;

        private bool inValueUpdate;

        protected override void SetupView() { 
            this.input.text = this.DataModel.Value;
            this.input.onValueChanged.AddListener(this.OnFieldChanged);
        }

        protected override void OnValueUpdated(string value) {
            this.inValueUpdate = true;
            this.input.text = value;
            this.inValueUpdate = false;
        }

        private void OnFieldChanged(string value) {
            if (this.inValueUpdate) return;
            this.DataModel.SetValue(value);
        }
    }
}
