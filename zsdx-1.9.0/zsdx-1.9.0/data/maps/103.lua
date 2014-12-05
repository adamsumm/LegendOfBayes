local map = ...
local game = map:get_game()
-- Dungeon 7 3F

local fighting_boss = false
local just_removed_special_torch = false
local special_torch_timer = nil

local door_properties = { -- properties of the 5 timed doors
  [door_a] = { x = 920, y = 640, delay = 12000 },
  [door_b] = { x = 864, y = 808, delay = 15000 },
  [door_c] = { x = 1024, y = 840, delay = 12000 },
  [door_d] = { x = 832, y = 936, delay = 4000 },
  [door_e] = { x = 976, y = 952, delay = 4000 }
}
local current_door = nil -- current door during a camera movement
local door_timers = {} -- doors that currently have a timer running
local arrows_timer

function map:on_started(destination)

  -- block fallen into the hole
  if game:get_value("b623") then
    nw_block:set_enabled(false)
  end

  -- NW door
  if game:get_value("b624") then
    map:set_doors_open("ne_door", true)
  end

  -- door A (timed doors)
  if game:get_value("b627") then
    door_a_switch:set_activated(true)
  end

  -- boss
  if boss ~= nil then
    boss:set_enabled(false)
  end
  map:set_doors_open("boss_door", true)
  if game:get_value("b625")
    and not game:get_value("b626") then
    -- boss killed, heart container not picked
    map:create_pickable{
      treasure_name = "heart_container",
      treasure_variant = 1,
      treasure_savegame_variable = "b626",
      x = 544,
      y = 789,
      layer = 0
    }
  end

  -- special torch door
  if game:get_value("b624") then
    ne_switch:set_activated(true)
  end
end

function nw_block:on_moved()

  local x, y = self:get_position()
  if x == 536 and y == 69 then
    -- make the block fall
    self:set_enabled(false)
    hole_a:set_enabled(true)
    hole_a_teletransporter:set_enabled(true)
    game:set_value("b623", true)
    sol.timer.start(500, function() sol.audio.play_sound("bomb") end)
  end
end

-- north-east room
function ne_switch:on_activated()

  current_door = nil
  map:move_camera(960, 312, 250, function()
    sol.audio.play_sound("secret")
    map:open_doors("ne_door")
  end)
end

-- switch that removes the special torch
function special_torch_switch:on_activated()

  current_door = nil
  map:move_camera(960, 120, 250, function()
    sol.audio.play_sound("secret")
    special_torch:set_enabled(false)
    just_removed_special_torch = true
  end)
end

-- timed doors
local function timed_door_switch_activated(switch)

  local door_name = switch:get_name():match("^(door_[a-e])_switch$")
  current_door = map:get_entity(door_name)
  if current_door ~= nil then
    local properties = door_properties[current_door]
    map:move_camera(properties.x, properties.y, 250, function()
      map:open_doors(door_name)
    end)
  end
end

-- pass a timed door
local function timed_door_done_sensor_activated(sensor)

  local door_name = sensor:get_name():match("^(door_[a-e])_done_sensor$")
  local door = map:get_entity(door_name)
  if door ~= nil then
    door_timers[door] = nil -- disable the timer
  end
end

-- close a timed door previously passed (i.e. it has no current timer)
local function timed_door_close_sensor_activated(sensor)

  local door_name = sensor:get_name():match("^(door_[a-e])_close_sensor$")
  local door = map:get_entity(door_name)

  if door ~= nil then
    if door_timers[door] == nil and not door:is_closed() then
      map:close_doors(door_name)
      map:get_entity(door_name .. "_switch"):set_activated(false)
    end
  end
end

for door, _ in pairs(door_properties) do
  local switch = map:get_entity(door:get_name() .. "_switch")
  local done_sensor = map:get_entity(door:get_name() .. "_done_sensor")
  local close_sensor = map:get_entity(door:get_name() .. "_close_sensor")
  switch.on_activated = timed_door_switch_activated
  if done_sensor ~= nil then
    done_sensor.on_activated = timed_door_done_sensor_activated
  end
  if close_sensor ~= nil then
    close_sensor.on_activated = timed_door_close_sensor_activated
  end
end

function map:on_camera_back()

  -- set up a timer when the camera movement is finished
  if just_removed_special_torch then
    just_removed_special_torch = false
    special_torch_timer = sol.timer.start(8000, function()
      sol.audio.play_sound("door_closed")
      special_torch:set_enabled(true)
      special_torch_switch:set_activated(false)
      special_torch_timer = nil
    end)
    special_torch_timer:set_with_sound(true)

  elseif current_door ~= nil then
    local door = current_door
    local door_name = door:get_name()
    local timer = sol.timer.start(door_properties[door].delay, function()
      if door_timers[door] ~= nil then
	map:close_doors(door_name)
	map:get_entity(door_name .. "_switch"):set_activated(false)
	door_timers[door] = nil
      end
    end)
    timer:set_with_sound(true)
    door_timers[door] = true
    current_door = nil

  end
end

-- special torch
function special_torch_dont_close_sensor:on_activated()

  if special_torch_timer ~= nil then
    special_torch_timer:stop()
    special_torch_timer = nil
  end
end

-- boss door
function close_boss_door_sensor:on_activated()

  if boss_door:is_open() and not game:get_value("b625") then
    -- The boss is alive.
    map:close_doors("boss_door")
    sol.audio.stop_music()
  end
end

-- boss
local function repeat_give_arrows()

  -- give arrows if necessary during the boss fight
  if game:get_item("bow"):get_amount() == 0 then
    local positions = {
      { x = 416, y = 685 },
      { x = 672, y = 685 },
      { x = 416, y = 885 },
      { x = 672, y = 885 },
    }
    arrow_xy = positions[math.random(#positions)]
    map:create_pickable{
      treasure_name = "arrow",
      treasure_variant = 3,
      x = arrow_xy.x,
      y = arrow_xy.y,
      layer = 0
    }
  end
  arrows_timer = sol.timer.start(20000, repeat_give_arrows)
end

function start_boss_sensor:on_activated()

  if not game:get_value("b625")
      and not fighting_boss then
    sol.audio.play_music("boss")
    boss:set_enabled(true)
    fighting_boss = true
    arrows_timer = sol.timer.start(20000, repeat_give_arrows)
  end
end

-- west room
function w_room_sensor:on_activated()

  sol.audio.play_sound("secret")
  local state = w_room_tile_1:is_enabled()
  w_room_tile_1:set_enabled(not state)
  w_room_tile_2:set_enabled(state)
end
w_room_sensor_2.on_activated = w_room_sensor.on_activated

function map:on_obtained_treasure(item, variant, savegame_variable)

  if item:get_name() == "heart_container" then
    sol.audio.play_music("victory")
    hero:freeze()
    hero:set_direction(3)
    sol.timer.start(9000, function()
      sol.audio.play_music("dungeon_finished")
      hero:set_direction(1)
      sahasrahla:set_position(544, 717)
      map:move_camera(544, 712, 100, function()
        game:start_dialog("dungeon_7.sahasrahla", game:get_player_name(), function()
          hero:start_victory(function()
            game:set_dungeon_finished(7)
            hero:teleport(8, "from_dungeon_7")
          end)
        end)
      end, 1000, 86400000)
    end)
  end
end

if boss ~= nil then
  function boss:on_dead()

    -- create the heart container manually to be sure it won't be in a hole
    map:create_pickable{
      treasure_name = "heart_container",
      treasure_variant = 1,
      treasure_savegame_variable = "b626",
      x = 544,
      y = 789,
      layer = 0
    }
    if arrows_timer ~= nil then
      arrows_timer:stop()
      arrows_timer = nil
    end
  end
end

