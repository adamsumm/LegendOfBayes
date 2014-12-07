local map = ...
local game = map:get_game()
-- Dungeon 3 2F

local remove_water_delay = 500 -- delay between each step when some water is disappearing
local remove_se_water
local remove_se_water_2
local remove_se_water_3
local remove_se_water_4
local remove_se_water_5
local remove_se_water_6

function remove_se_water()
  sol.audio.play_sound("water_drain_begin")
  sol.audio.play_sound("water_drain")
  se_water_tile_out:set_enabled(true)
  se_water_tile_source:set_enabled(false)
  sol.timer.start(remove_water_delay, remove_se_water_2)
end

function remove_se_water_2()
  se_water_tile_middle:set_enabled(false)
  sol.timer.start(remove_water_delay, remove_se_water_3)
end

function remove_se_water_3()
  se_water_tile_initial:set_enabled(false)
  se_water_tile_less_a:set_enabled(true)
  sol.timer.start(remove_water_delay, remove_se_water_4)
end

function remove_se_water_4()
  se_water_tile_less_a:set_enabled(false)
  se_water_tile_less_b:set_enabled(true)
  sol.timer.start(remove_water_delay, remove_se_water_5)
end

function remove_se_water_5()
  se_water_tile_less_b:set_enabled(false)
  se_water_tile_less_c:set_enabled(true)
  sol.timer.start(remove_water_delay, remove_se_water_6)
end

function remove_se_water_6()
  se_water_tile_less_c:set_enabled(false)
  map:set_entities_enabled("se_water_on_jumper", false)
  map:set_entities_enabled("se_water_off_obstacle", true)
  game:set_value("b128", true)
  sol.audio.play_sound("secret")
end

local function remove_1f_n_water()

  sol.audio.play_sound("water_drain_begin")
  sol.audio.play_sound("water_drain")
  game:start_dialog("dungeon_3.water_drained_somewhere")
  game:set_value("b131", true)
end

local function remove_1f_e_water()

  sol.audio.play_sound("water_drain_begin")
  sol.audio.play_sound("water_drain")
  game:start_dialog("dungeon_3.water_drained_somewhere")
  game:set_value("b122", true)
end

function map:on_started(destination)

  if game:get_value("b127") then
    -- the barrier of the compass chest is removed
    barrier_tile:set_enabled(false)
    barrier_switch:set_activated(true)
  end

  if game:get_value("b128") then
    -- the south-east water is drained
    map:set_entities_enabled("se_water_tile", false)
    map:set_entities_enabled("se_water_tile_out", true)
    map:set_entities_enabled("se_water_on_jumper", false)
  else
    map:set_entities_enabled("se_water_off_obstacle", false)
  end

  if game:get_value("b908") then
    -- shortcut A
    map:set_entities_enabled("shortcut_a_tile", false)
    shortcut_a_switch:set_activated(true)
  end

  if game:get_value("b909") then
    -- shortcut B
    map:set_entities_enabled("shortcut_b_tile", false)
    shortcut_b_switch:set_activated(true)
  end

  -- north chest
  if game:get_value("b950") then
    n_switch:set_activated(true)
  else
    n_chest:set_enabled(false)
  end
end

-- Called when the opening transition of the map finished
function map:on_opening_transition_finished(destination)

  -- show the welcome message
  if destination == from_outside then
    game:start_dialog("dungeon_3")
  end
end

for enemy in map:get_entities("e_room_enemy") do
  function enemy:on_dead()

    if not map:has_entities("e_room_enemy")
        and not e_door:is_open() then
      map:move_camera(856, 472, 250, function()
        sol.audio.play_sound("secret")
        map:open_doors("e_door")
      end)
    end
  end
end

function barrier_switch:on_activated()

  if barrier_tile:is_enabled() then
    map:move_camera(120, 240, 250, function()
      sol.audio.play_sound("secret")
      barrier_tile:set_enabled(false)
      game:set_value("b127", true)
    end)
  end
end

function se_water_switch:on_activated()

  if not game:get_value("b128") then
    map:move_camera(912, 896, 250, remove_se_water, 1000, 3500)
  end
end

function n_1f_water_switch:on_activated()

  if not game:get_value("b131") then
    remove_1f_n_water()
  end
end

function e_1f_water_switch_1:on_activated()

  if e_1f_water_switch_2:is_activated()
      and not game:get_value("b122") then
    remove_1f_e_water()
  end
end

function e_1f_water_switch_2:on_activated()

  if e_1f_water_switch_1:is_activated()
      and not game:get_value("b122") then
    remove_1f_e_water()
  end
end

function shortcut_a_switch:on_activated()

  map:set_entities_enabled("shortcut_a_tile", false)
  game:set_value("b908", true)
  sol.audio.play_sound("secret")
end

function shortcut_b_switch:on_activated()

  map:set_entities_enabled("shortcut_b_tile", false)
  game:set_value("b909", true)
  sol.audio.play_sound("secret")
end

function n_switch:on_activated()

  map:move_camera(280, 56, 250, function()
    sol.audio.play_sound("chest_appears")
    n_chest:set_enabled(true)
    game:set_value("b950", true)
  end)
end

