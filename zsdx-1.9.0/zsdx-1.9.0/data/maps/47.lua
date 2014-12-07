local map = ...
local game = map:get_game()
-- Dungeon 8 1F

-- Legend
-- RC: Rupee Chest
-- KC: Key Chest
-- KP: Key Pot
-- LD: Locked Door
-- KD: Key Door
-- DB: Door Button
-- LB: Locked Barrier
-- BB: Barrier Button
-- DS: Door Sensor

local fighting_boss = false

function map:on_started(destination)
  map:set_doors_open("LD1", true)
  map:set_doors_open("LD3", true)
  map:set_doors_open("LD4", true)
  billy_npc:set_enabled(false)
  map:set_doors_open("boss_door", true)

  -- Hide the map chest if not already opened
  if not game:get_value("b700") then
    MAP:set_enabled(false)
  end

  -- Hide the big key chest if not already opened
  if not game:get_value("b705") then
    BK01:set_enabled(false)
  end

  -- Link has the mirror shield: no laser obstacles
  if game:get_ability("shield") >= 3 then
    LO1:set_enabled(false)
    map:set_entities_enabled("LO2", false)
  end

  if destination == from_boss or destination == from_hidden_room then
    map:set_doors_open("LD5", true)
  end

  if destination == from_hidden_room then
    map:remove_entities("room_LD5_enemy")
  end

  -- door to Agahnim open if Billy's heart container was picked
  if game:get_value("b729") then
    map:set_doors_open("agahnim_door", true)
  end

  -- statues puzzle
  if game:get_value("b723") then
    DB06:set_activated(true)
  end

  -- boss key door and laser
  if game:get_value("b730") then
    boss_key_door_laser:remove()
  end

  if boss ~= nil then
    boss:set_enabled(false)
  end
end

function map:on_opening_transition_finished(destination)
  if destination == from_outside then
    game:start_dialog("dungeon_8.welcome")
  end
end

function BB1:on_activated()

  -- LB1 room
  map:move_camera(896, 1712, 250, function()
    LB1:set_enabled(false)
    sol.audio.play_sound("secret")
  end)
end

function BB2:on_activated()

  -- LB2 room
  LB2:set_enabled(false)
  sol.audio.play_sound("secret")
end

function DB4:on_activated()

  map:open_doors("LD4")
  sol.audio.play_sound("secret")
end

function DB06:on_activated()

  -- 4 statues room door opening
  map:open_doors("LD6")
  sol.audio.play_sound("secret")
end

for switch in map:get_entities("RPS") do

  function switch:on_activated()
    -- Resets position of statues
    STT1:reset()
    STT2:reset()
    STT3:reset()
    sol.audio.play_sound("warp")
  end
end

function DS1:on_activated()

  -- LD1 room: when Link enters this room, door LD1 closes and enemies appear, sensor DS1 is disabled
  if map:has_entities("room_LD1_enemy") then
    map:close_doors("LD1")
    DS1:set_enabled(false)
  end
end

function DS3:on_activated()

  if map:has_entities("map_enemy") then
    map:close_doors("LD3")
    DS3:set_enabled(false)
  end
end

function DS4:on_activated()

  map:close_doors("LD4")
  DS4:set_enabled(false)
end

function start_boss_sensor:on_activated()

  if not fighting_boss and not game:get_value("b727") then
    sol.audio.stop_music()
    map:close_doors("boss_door")
    billy_npc:set_enabled(true)
    hero:freeze()
    fighting_boss = true
    sol.timer.start(1000, function()
      game:start_dialog("dungeon_8.billy", function()
        sol.audio.play_music("boss")
        hero:unfreeze()
        boss:set_enabled(true)
        billy_npc:set_enabled(false)
      end)
    end)
  end
end

for enemy in map:get_entities("room_LD1_enemy") do
  
  function enemy:on_dead()
    if not map:has_entities("room_LD1_enemy") then
      -- LD1 room: kill all enemies will open the doors LD1 and LD2
      if not LD1:is_open() then
        map:open_doors("LD1")
        map:open_doors("LD2")
        sol.audio.play_sound("secret")
      end
    end
  end
end

for enemy in map:get_entities("room_LD5_enemy") do
  
  function enemy:on_dead()

    if not map:has_entities("room_LD5_enemy") and not LD5:is_open() then
      -- LD5 room: kill all enemies will open the door LD5
      map:open_doors("LD5")
      sol.audio.play_sound("secret")
    end
  end
end

for enemy in map:get_entities("map_enemy") do
  
  function enemy:on_dead()

    if not map:has_entities("map_enemy") then
      -- Map chest room: kill all enemies and the chest will appear
      if not game:get_value("b700") then
        MAP:set_enabled(true)
        sol.audio.play_sound("chest_appears")
      elseif not LD3:is_open() then
        sol.audio.play_sound("secret")
      end
      map:open_doors("LD3")
    end
  end
end

for enemy in map:get_entities("room_big_enemy") do
  
  function enemy:on_dead()

    if not map:has_entities("room_big_enemy") then
      -- Big key chest room: kill all enemies and the chest will appear
      if not game:get_value("b705") then
        BK01:set_enabled(true)
        sol.audio.play_sound("chest_appears")
      end
    end
  end
end

function map:on_obtained_treasure(item, variant, savegame_variable)

  if item:get_name() == "heart_container" then
    sol.audio.play_music("victory")
    hero:freeze()
    hero:set_direction(3)
    sol.timer.start(9000, function()
      map:open_doors("boss_door")
      map:open_doors("agahnim_door")
      sol.audio.play_sound("secret")
      hero:unfreeze()
    end)
  end
end

function boss_key_door:on_opened()

  boss_key_door_laser:remove()
end

function WW01:on_opened()

  sol.audio.play_sound("secret")
end

