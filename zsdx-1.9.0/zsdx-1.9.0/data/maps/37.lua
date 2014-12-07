local map = ...
local game = map:get_game()
-- Billy's cave

local billy_leave_step = 0

local function billy_leave()

  billy_leave_step = billy_leave_step + 1
  local sprite = billy:get_sprite()

  if billy_leave_step == 1 then
    hero:freeze()
    local m = sol.movement.create("path")
    m:set_path{4,4,4,4,4,4,4}
    m:set_speed(48)
    m:set_ignore_obstacles(true)
    m:start(billy)
  elseif billy_leave_step == 2 then
    sprite:set_direction(1)
    sol.timer.start(500, billy_leave)
  elseif billy_leave_step == 3 then
    map:open_doors("door")
    sol.timer.start(500, billy_leave)
  elseif billy_leave_step == 4 then
    local m = sol.movement.create("path")
    m:set_path{2,2,2,2,2,2,2,2}
    m:set_speed(48)
    m:start(billy)
  else
    map:close_doors("door")
    billy:remove()
    hero:unfreeze()
  end
end

local function give_croissant()

  if game:get_item("croissants_counter"):has_amount(1) then
    game:start_dialog("billy_cave.give_croissant")
  else
    game:start_dialog("billy_cave.give_croissant_without")
  end
end

local function give_apple_pie()
  if game:get_item("level_4_way"):get_variant() == 1 then
    game:start_dialog("billy_cave.give_apple_pie")
  else
    game:start_dialog("billy_cave.give_apple_pie_without")
  end
end

local function give_golden_bars()
  game:start_dialog("billy_cave.give_golden_bars", function()
    hero:start_treasure("level_4_way", 3, "b134", function()
      -- got the edelweiss: make Billy leave
      billy_leave()
    end)
  end)
end

function map:on_started(destination)

  if game:get_value("b134") then
    -- the player has already given the golden bars and obtained the edelweiss
    billy:remove()
  end

  if game:get_value("b928") then
    map:set_doors_open("door", true)
  end
end

function billy:on_interaction()

  if not game:get_value("b135") then
    game:start_dialog("billy_cave.hello")
    game:set_value("b135", true)
  else
    game:start_dialog("billy_cave.what_do_you_have", function()
      if game:get_item("level_4_way"):get_variant() == 2 then
        -- the player has the golden bars
        game:start_dialog("billy_cave.with_golden_bars", function(answer)
          if answer == 1 then
            give_golden_bars()
          else
            give_apple_pie()
          end
        end)
      else
        game:start_dialog("billy_cave.without_golden_bars", function(answer)
          if answer == 1 then
            give_croissant()
          else
            give_apple_pie()
          end
        end)
      end
    end)
  end
end

function billy:on_movement_finished()

  billy_leave()
end

function save_solid_ground_sensor:on_activated()
  hero:save_solid_ground()
end

function save_solid_ground_sensor_2:on_activated()
  hero:save_solid_ground()
end

