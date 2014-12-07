local map = ...
local game = map:get_game()
-- Outside world A1

local function remove_dungeon_7_lock()
  dungeon_7_lock:remove()
  map:set_entities_enabled("dungeon_7_lock_tile", false)
end

function map:on_started(destination)

  -- game ending sequence
  if destination == from_ending then
    hero:freeze()
    hero:set_visible(false)
    game:set_hud_enabled(false)
    map:set_entities_enabled("enemy", false)
    sol.audio.play_music("fanfare")
  else
    -- enable dark world
    if game:get_value("b905") then
      map:set_tileset(13)
    end
  end

  -- remove the dungeon 7 lock if open
  if game:get_value("b919") then
    remove_dungeon_7_lock()
  end
end

function map:on_opening_transition_finished(destination)

  if destination == from_ending then
    game:start_dialog("credits_1", function()
      sol.timer.start(2000, function()
	hero:teleport(4, "from_ending")
      end)
    end)
    map:move_camera(1000, 240, 25, function() end, 1e6)
  end
end

function dungeon_7_lock:on_interaction()

  -- open the door if the player has the ice key
  if game:has_item("ice_key") then
    sol.audio.play_sound("door_open")
    sol.audio.play_sound("secret")
    game:set_value("b919", true)
    remove_dungeon_7_lock()
  else
    game:start_dialog("outside_world.ice_key_required")
  end
end

