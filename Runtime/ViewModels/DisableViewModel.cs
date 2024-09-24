using Kuuasema.DataBinding;
namespace Kuuasema.Core {
    public class DisableViewModel : ViewModel<bool> {

        private bool isDisabled;
        protected virtual bool ToggleGameObject => true;

        protected override void SetupView() { 
            this.isDisabled = this.DataModel.Value;
            this.HandleDisabled();
        }

        protected override void OnValueUpdated(bool value) {
            if (this.isDisabled != value) {
                this.isDisabled = value;
                this.HandleDisabled();
            }
        }

        private void HandleDisabled() {
            if (this.isDisabled) {
                this.OnDisableView();
            } else {
                this.OnEnableView();
            }
            if (this.ToggleGameObject && this.gameObject.activeSelf == this.isDisabled) {
                this.gameObject.SetActive(!this.isDisabled);
            }
        }

        protected virtual void OnEnableView() {
            
        }
        protected virtual void OnDisableView() {
            
        }
    }
}
