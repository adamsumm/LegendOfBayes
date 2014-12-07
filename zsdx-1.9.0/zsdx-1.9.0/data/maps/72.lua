local map = ...
-- Smith cave (battle against the thiefs)

function map:on_started(destination)

  map:set_doors_open("door", true)
  map:set_entities_enabled("enemy", false)
end

function close_door_sensor:on_activated()

  if door:is_open()
      and not map:get_game():get_value("b156") then
    map:close_doors("door")
    hero:freeze()
    sol.timer.start(1000, function()
      sol.audio.play_music("soldiers")
      sol.timer.start(1500, function()
        map:set_entities_enabled("enemy", true)
        hero:unfreeze()
      end)
    end)
  end
end

local function enemy_dead(enemy)

  if not map:has_entities("enemy") then
    hero:start_victory(function()
      map:get_game():set_value("b156", true)
      hero:teleport(3, "out_smith_cave")
    end)
  end
end
for enemy in map:get_entities("enemy") do
  enemy.on_dead = enemy_dead
end

