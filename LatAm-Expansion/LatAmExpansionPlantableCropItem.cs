using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace LatAm_Expansion;

public class LatAmExpansionPlantableCropItem : Item
{
    private WorldInteraction[]? interactions;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api.Side != EnumAppSide.Client)
        {
            return;
        }

        interactions = ObjectCacheUtil.GetOrCreate(api, "latamExpansionPlantableCropInteractions", () =>
        {
            List<ItemStack> farmlandStacks = new();
            foreach (Block block in api.World.Blocks)
            {
                if (block.Code == null || block.EntityClass == null)
                {
                    continue;
                }

                if (api.World.ClassRegistry.GetBlockEntity(block.EntityClass) == typeof(BlockEntityFarmland))
                {
                    farmlandStacks.Add(new ItemStack(block));
                }
            }

            return new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "heldhelp-plant",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = farmlandStacks.ToArray()
                }
            };
        });
    }

    public override void OnHeldInteractStart(
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handHandling)
    {
        if (blockSel == null)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            return;
        }

        BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (blockEntity is not BlockEntityFarmland farmland)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            return;
        }

        string cropCode = Attributes?["plantCropCode"].AsString() ?? "";
        if (cropCode.Length == 0)
        {
            return;
        }

        Block? cropBlock = byEntity.World.GetBlock(CodeWithPath("crop-" + cropCode + "-1"));
        if (cropBlock == null)
        {
            return;
        }

        IPlayer? player = (byEntity as EntityPlayer)?.Player;
        bool planted = farmland.TryPlant(cropBlock, itemslot, byEntity, blockSel);
        if (!planted)
        {
            return;
        }

        byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), blockSel.Position, 0.4375, player, true, 32f, 1f);
        ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

        if (player?.WorldData.CurrentGameMode != EnumGameMode.Creative)
        {
            itemslot.TakeOut(1);
            itemslot.MarkDirty();
        }

        handHandling = EnumHandHandling.PreventDefault;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        string cropCode = Attributes?["plantCropCode"].AsString() ?? "";
        if (cropCode.Length == 0)
        {
            return;
        }

        Block? block = world.GetBlock(CodeWithPath("crop-" + cropCode + "-1"));
        if (block?.CropProps == null)
        {
            return;
        }

        dsc.AppendLine(Lang.Get("soil-nutrition-requirement") + block.CropProps.RequiredNutrient);
        dsc.AppendLine(Lang.Get("soil-nutrition-consumption") + block.CropProps.NutrientConsumption);

        double days = block.CropProps.TotalGrowthDays;
        if (days <= 0)
        {
            days = block.CropProps.TotalGrowthMonths * world.Calendar.DaysPerMonth;
        }

        days /= api.World.Config.GetDecimal("cropGrowthRateMul", 1);
        dsc.AppendLine(Lang.Get("soil-growth-time") + " " + Lang.Get("count-days", Math.Round(days, 1)));
        dsc.AppendLine(Lang.Get("crop-coldresistance", Math.Round(block.CropProps.ColdDamageBelow, 1)));
        dsc.AppendLine(Lang.Get("crop-heatresistance", Math.Round(block.CropProps.HeatDamageAbove, 1)));
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        return interactions == null
            ? base.GetHeldInteractionHelp(inSlot)
            : interactions.Append(base.GetHeldInteractionHelp(inSlot));
    }
}
