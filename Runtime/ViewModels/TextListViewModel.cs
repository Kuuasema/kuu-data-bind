using Kuuasema.DataBinding;
namespace Kuuasema.Core {
    public class TextListViewModel : ListViewModel<string, DataModel<string>, TextViewModel> {
        // [ViewBind("Template")]
        // private TextMeshProUGUI textTemplate;
        // private List<TextMeshProUGUI> listElements = new List<TextMeshProUGUI>();


        // protected override void BuildList() {
        //     List<string> list = this.DataModel.Value;
        //     int count = list.Count;
        //     if (this.listElements.Count != count) {
        //         while (this.listElements.Count < count) {
        //             TextMeshProUGUI element = Instantiate(this.textTemplate.gameObject, this.textTemplate.transform.parent).GetComponent<TextMeshProUGUI>();
        //             element.gameObject.SetActive(true);
        //             this.listElements.Add(element);
        //         }
        //         for (int i = this.listElements.Count - 1; i >= count; i--) {
        //             this.listElements[i].gameObject.SetActive(false);
        //         }
        //     }
        //     for (int i = 0; i < count; i++) {
        //         this.listElements[i].text = list[i];
        //         this.listElements[i].gameObject.SetActive(true);
        //     }
        // }
    }
}