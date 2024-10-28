using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Kuuasema.DataBinding;

public enum Season {
    Summer,
    Autumn
}

public class SeasonDataModel : DataModel<Season> {
    
}

public class SeasonViewModel : ViewModel<Season,SeasonDataModel> {

}

