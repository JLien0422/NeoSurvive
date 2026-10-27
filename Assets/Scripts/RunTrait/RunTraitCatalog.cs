using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RunTraitCatalog", menuName = "NeoSurvive/Traits/Run Trait Catalog")]
public class RunTraitCatalog : ScriptableObject
{
    public List<RunTraitDefinition> traits = new List<RunTraitDefinition>();
}
