local map = ...
local game = map:get_game()
local game = map:get_game()
-- Dungeon 4 1F

local fighting_boss = false

function map:on_started(destination)

  if boss ~= nil then
    boss:set_enabled(false)
  end

  if game:get_value("b412") then
    weak_floor:set_enabled(false)
    weak_floor_sensor:set_enabled(false)
  else
    weak_floor_teletransporter:set_enabled(false)
  end

  map:set_doors_open("boss_door", true)
  map:set_doors_open("final_room_door", true)
end

function map:on_opening_transition_finished(destination)

  -- show the welcome message
  if destination == from_outside then
    game:start_dialog("dungeon_4.welcome")
  end
end

function hidden_weak_wall:on_opened()

  sol.audio.play_sound("secret")
end

function weak_block:on_opened()

  sol.audio.play_sound("secret")
end

function weak_floor_sensor:on_collision_explosion()

  if weak_floor:is_enabled() then

    weak_floor:set_enabled(false)
    weak_floor_sensor:set_enabled(false)
    weak_floor_teletransporter:set_enabled(true)
    sol.audio.play_sound("secret")
    game:set_value("b412", true)
  end
end

function start_boss_sensor:on_activated()

  if not game:get_value("b415")
      and not fighting_boss then
    map:close_doors("boss_door")
    map:close_doors("final_room_door")
    hero:freeze()
    sol.timer.start(1000, function()
      sol.audio.play_music("boss")
      boss:set_enabled(true)
      hero:unfreeze()
    end)
    fighting_boss = true
  end
end

function map:on_obtained_treasure(item, variant, savegame_variable)

  if item:get_name() == "heart_container" then
    sol.timer.start(9000, function()
      map:open_doors("final_room_door")
      sol.audio.play_sound("secret")
      hero:unfreeze()
    end)
    sol.audio.play_music("victory")
    hero:freeze()
    hero:set_direction(3)
  end
end

