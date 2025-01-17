using System;
using TMPro;
namespace Kuuasema.DataBinding {
    public class FormattedIntegerViewModel : ViewModel<int> {
        public string Format;
        [ViewBind]
        protected TextMeshProUGUI text;
        public TextMeshProUGUI Text => this.text;

        protected override void SetupView() { 
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }

        protected override void OnValueUpdated(int value) {
            this.text.text = String.Format(this.Format, this.DataModel.Value);
        }
    }
}
