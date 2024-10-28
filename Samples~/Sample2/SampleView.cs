using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kuuasema.Utils;
using Kuuasema.DataBinding;

namespace Sample2 {

    public class SampleView : ViewModel<SampleView.Data, SampleView.Model> {
        public class Data {
            [DataBind] string Text;
        }

        public class Model : DataModel<Data> {
            [PropertyBind] public DataModel<string> Text { get; private set; }
        }
        
        protected override bool CreateDataOnStart => true;
        [ViewBind] [SerializeField] protected List<ViewModel<string>> Text;
    }
}