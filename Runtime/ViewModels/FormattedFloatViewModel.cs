using System;
using TMPro;
namespace Kuuasema.DataBinding {
    public class FormattedFloatViewModel : ViewModel<float> {
        public string Format;
        [ViewBind]
        private TextMeshProUGUI text;
        public TextMeshProUGUI Text => this.text;

        protected override void SetupView() { 
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }

        protected override void OnValueUpdated(float value) {
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }
    }
}
