using Vintagestory.API.Common;

namespace LatAm_Expansion;

public class LatAmExpansionModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("LatAmExpansionPlantableCropItem", typeof(LatAmExpansionPlantableCropItem));
    }
}
