local map = ...
local game = map:get_game()
-- Outside world B1

local function show_vine()

  map:move_camera(608, 136, 136, function()
    sol.audio.play_sound("secret")
    hero:unfreeze()
    game:set_value("b921", true)
  end)

  local sprite = vine:get_sprite()
  sprite:set_ignore_suspend(true)
  sprite:set_paused(false)
end

function map:on_started(destination)

  -- enable dark world
  if game:get_value("b905") then
    map:set_tileset(13)
  end

  if game:get_value("b921") then
    -- show the vine
    vine_start:remove()
    vine:remove()
    map:set_entities_enabled("vine", false)
  else
    vine:get_sprite():set_paused(true)
  end
end

-- Function called when the player presses the action key on the vine bottom
function vine_start:on_interaction()
  game:start_dialog("outside_world.vine_start")
end

-- Function called when the player uses an item on the vine bottom
function vine_start:on_interaction_item(item)

  if item:get_name():find("^bottle") and item:get_variant() == 2 then

    -- using water on the vine bottom
    hero:freeze()
    self:remove()
    item:set_variant(1)  -- make the bottle empty
    game:set_value("b921", true)
    sol.audio.play_sound("item_in_water")
    sol.timer.start(1000, show_vine)
    return true
  end

  return false
end

