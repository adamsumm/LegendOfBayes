local map = ...
local game = map:get_game()
-- Outside world A2

local fighting_boss = false -- Agahnim

function map:on_started(destination)

  local new_music

  if boss ~= nil then
    boss:set_enabled(false)
  end

  if game:get_value("b905") then
    -- enable dark world
    new_music = "dark_world"
    map:set_tileset(13)
    map:set_entities_enabled("castle_east_bridge", false)
    map:set_entities_enabled("castle_east_bridge_off", true)

    if game:get_value("b907") then
      castle_door_switch:set_activated(true)
    else
      castle_door:set_enabled(true)
    end

    map:set_entities_enabled("teletransporter_lw", false)

    -- Agahnim fight
    if destination == from_dungeon_5_2F_ne
        and game:get_value("b507")
        and not game:get_value("b520") then

      new_music = nil
      cannon:remove()
      map:set_entities_enabled("enemy", false) -- disable all simple enemies
    end

  else
    new_music = "overworld"
    map:set_entities_enabled("castle_east_bridge_off", false)
    map:set_entities_enabled("teletransporter_dw", false)
  end

  sol.audio.play_music(new_music)
end

function castle_door_switch:on_activated()

  map:move_camera(296, 552, 250, function()
    castle_door:set_enabled(false)
    game:set_value("b907", true)
    sol.audio.play_sound("secret")
    sol.audio.play_sound("door_open")
  end)
end

function cannon:on_interaction()

  if not game:get_value("b905") then
    game:start_dialog("castle.cannon")
  else
    hero:freeze()
    local x, y = self:get_position()
    hero:set_position(x, y, 0)
    hero:set_visible(false)
    sol.audio.play_sound("bomb")
    sol.timer.start(2000, function()
      sol.audio.play_sound("explosion")
      map:create_explosion{
	x = 296,
	y = 384,
	layer = 0
      }
      hero:start_jumping(6, 424, true)
      hero:set_visible(true)
    end)
  end
end

function start_boss_sensor:on_activated()

  if game:get_value("b507")
      and not game:get_value("b520")
      and not fighting_boss then

    -- Agahnim fight
    hero:freeze()
    map:set_entities_enabled("castle_roof_entrance", false)
    map:set_entities_enabled("castle_roof_stairs", false)
    map:set_entities_enabled("teletransporter_dw_roof", false)
    sol.audio.play_sound("door_closed")
    sol.timer.start(1000, function()
      sol.audio.play_music("ganon_appears")
      boss:set_enabled(true)
      game:start_dialog("dungeon_5.agahnim_beginning", function()
	sol.audio.play_music("ganon_battle")
      end)
      hero:unfreeze()
      fighting_boss = true
    end)
  end
end

function map:on_obtained_treasure(item, variant, savegame_variable)

  if item:get_name() == "heart_container" then
    game:set_dungeon_finished(5)
    sol.timer.start(9000, function()
      hero:teleport(9, "from_dungeon_5_1F")
      sol.timer.start(700, function()
	sol.audio.play_music("dark_world")
      end)
    end)
    sol.audio.play_music("victory")
    hero:freeze()
    hero:set_direction(3)
  end
end

