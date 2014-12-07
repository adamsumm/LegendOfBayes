local map = ...
local game = map:get_game()
-- Potion shop

local function potion_buying(shop_item)

  local bottle_2 = game:get_item("bottle_2")
  if not game:get_value("b911")
      and not bottle_2:has_variant() then
    -- give bottle 2
    bottle_2:set_variant(1)
  end

  if game:get_first_empty_bottle() == nil then
    game:start_dialog("potion_shop.no_empty_bottle")
    return false
  end

  return true
end
red_potion.on_buying = potion_buying
green_potion.on_buying = potion_buying
blue_potion.on_buying = potion_buying

function map:on_obtained_treasure(item, variant, savegame_variable)

  if item:get_name():find("_potion$")
      and not game:get_value("b911") then
    -- tell the player we juste gave him the bottle 2
    game:set_value("b911", true)
    game:start_dialog("potion_shop.give_bottle")
  end
end

function witch:on_interaction()

  if not game:get_value("b911") then
    game:start_dialog("potion_shop.witch_bottle_offered")
  else
    game:start_dialog("potion_shop.witch")
  end
end

