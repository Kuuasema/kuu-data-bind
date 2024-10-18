using Kuuasema.DataBinding;
namespace Kuuasema.Core {
    public class EnableViewModel : ViewModel<bool> {

        private bool isEnabled;
        protected virtual bool ToggleGameObject => true;

        protected override void SetupView() { 
            this.isEnabled = this.DataModel.Value;
            this.HandleEnabled();
        }

        protected override void OnValueUpdated(bool value) {
            if (this.isEnabled != value) {
                this.isEnabled = value;
                this.HandleEnabled();
            }
        }

        private void HandleEnabled() {
            if (this.isEnabled) {
                this.OnEnableView();
            } else {
                this.OnDisableView();
            }
            if (this.ToggleGameObject && this.gameObject.activeSelf != this.isEnabled) {
                this.gameObject.SetActive(this.isEnabled);
            }
        }

        protected virtual void OnEnableView() {
            
        }
        protected virtual void OnDisableView() {
            
        }
    }
}
