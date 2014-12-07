local map = ...
local game = map:get_game()
-- Dungeon 1 2F

local fighting_miniboss = false

function map:on_started(destination)

  if khorneth ~= nil then
    khorneth:set_enabled(false)
  end

  if not boss_key_chest:is_open() then
    boss_key_chest:set_enabled(false)
  end
  map:set_doors_open("stairs_door", true)
  map:set_doors_open("miniboss_door", true)
end

function map:on_opening_transition_finished(destination)

  -- show the welcome message
  if destination == from_outside then
    game:start_dialog("dungeon_1")
  end
end

function start_miniboss_sensor:on_activated()

  if not game:get_value("b62") and not fighting_miniboss then
    -- the miniboss is alive
    map:close_doors("miniboss_door")
    hero:freeze()
    fighting_miniboss = true
    sol.timer.start(1000, function()
      sol.audio.play_music("boss")
      khorneth:set_enabled(true)
      hero:unfreeze()
    end)
  end
end

if khorneth ~= nil then
  function khorneth:on_dead()

    sol.audio.play_music("light_world_dungeon")
    map:open_doors("miniboss_door")
  end
end

for enemy in map:get_entities("boss_key_battle") do
  function enemy:on_dead()
    if not map:has_entities("boss_key_battle")
        and not boss_key_chest:is_enabled() then
      map:move_camera(104, 72, 250, function()
        sol.audio.play_sound("chest_appears")
        boss_key_chest:set_enabled(true)
      end)
    end
  end
end

