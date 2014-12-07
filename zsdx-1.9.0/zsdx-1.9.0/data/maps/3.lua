local map = ...
local game = map:get_game()

-- Outside world A3.

local monkey_sprite = nil
local monkey_jumps = 0
local monkey_jump_speed = 48

local function random_walk(npc)

  local m = sol.movement.create("random_path")
  m:set_speed(32)
  m:start(npc)
end

local function remove_dungeon_2_door()
  dungeon_2_door:remove()
  dungeon_2_door_tile:set_enabled(false)
end

-- Function called when the map starts.
-- The NPCs are initialized.
function map:on_started(destination)

  -- enable dark world
  if game:get_value("b905") then
    sol.audio.play_music("dark_world")
    map:set_tileset(13)
  else
    sol.audio.play_music("overworld")
  end

  if game:get_value("b24") then
    -- remove the monkey from Link's house entrance
    monkey:remove()
  else
    monkey_sprite = monkey:get_sprite()
  end

  if game:get_value("b89") then
    -- remove the dungeon 2 door
    remove_dungeon_2_door()
  end

  -- initialize NPCs
  random_walk(hat_man)
  random_walk(grand_son)

  if game:is_dungeon_finished(1) then
    how_to_save_npc:remove()
  else
    random_walk(how_to_save_npc)
  end

  -- smith cave with thiefs
  if game:get_value("b155") and not game:get_value("b156") then
    to_smith_cave:set_enabled(false)
  else
    to_smith_cave_thiefs:set_enabled(false)
  end

  -- entrances of houses
  local entrance_names = {
    "sahasrahla", "shop", "cake_shop", "hero", "telepathic_booth"
  }
  for _, entrance_name in ipairs(entrance_names) do
    local sensor = map:get_entity(entrance_name .. "_door_sensor")
    local tile = map:get_entity(entrance_name .. "_door")

    sensor.on_activated_repeat = function()
      if hero:get_direction() == 1
        and tile:is_enabled() then
        tile:set_enabled(false)
        sol.audio.play_sound("door_open")
      end
    end
  end
end

function monkey:on_interaction()

  if not game:get_value("b24") then
    -- monkey first dialog
    sol.audio.play_sound("monkey")
    game:start_dialog("outside_world.village.monkey", function()

      -- show another message depending on the shield
      if not game:has_ability("shield") then
	game:start_dialog("outside_world.village.monkey.without_shield")
      else
	game:start_dialog("outside_world.village.monkey.with_shield", function()
	  -- make the monkey leave
	  hero:freeze()
	  sol.audio.play_sound("monkey")
	  local m = sol.movement.create("jump")
	  m:set_direction8(1)
	  m:set_distance(24)
	  m:set_ignore_obstacles(true)
	  m:set_speed(monkey_jump_speed)
          m:start(monkey)
	  monkey_sprite:set_animation("jumping")
	  monkey_jumps = 1
	  game:set_value("b24", true)
	end)
      end
    end)
  else
    sol.audio.play_sound("monkey")
    game:start_dialog("outside_world.dungeon_2_entrance.monkey")
  end
end

function dungeon_2_door:on_interaction()

  -- open the door if the player has the Rock Key
  if game:has_item("rock_key") then
    sol.audio.play_sound("door_open")
    sol.audio.play_sound("secret")
    game:set_value("b89", true)
    remove_dungeon_2_door()
  else
    game:start_dialog("outside_world.rock_key_required")
  end
end

function hat_man:on_interaction()

  if game:is_dungeon_finished(1) then
    game:start_dialog("outside_world.village.hat_man_npc_waterfall")
  else
    game:start_dialog("outside_world.village.hat_man_npc")
  end
end

function tree_woman:on_interaction()

  game:start_dialog("outside_world.village.tree_woman", function(answer)
    if answer ~= "skipped" then
      hero:start_treasure("rupee", 1)
    end
  end)
end

-- Function called when the monkey has finished jumping
function monkey:on_movement_finished()

  if monkey_jumps == 1 then
    -- first jump finished: wait a little amount of time before jumping again
    sol.timer.start(300, function()
      -- start the second jump
      sol.audio.play_sound("monkey")
      local m = sol.movement.create("jump")
      m:set_direction8(2)
      m:set_distance(56)
      m:set_ignore_obstacles(true)
      m:set_speed(monkey_jump_speed)
      m:start(monkey)
      monkey_sprite:set_animation("jumping")
      monkey_jumps = 2
    end)
  elseif monkey_jumps == 2 then
    -- second jump finished: start the last jump
    sol.audio.play_sound("monkey")
    local m = sol.movement.create("jump")
    m:set_direction8(1)
    m:set_distance(64)
    m:set_ignore_obstacles(true)
    m:set_speed(monkey_jump_speed)
    m:start(monkey)
    monkey_sprite:set_animation("jumping")
    monkey_jumps = 3
  else
    -- last jump finished: remove the monkey from the map and unfreeze the hero
    monkey:remove()
    hero:unfreeze()
  end
end

function dungeon_3_entrance_weak_block:on_opened()
  sol.audio.play_sound("secret") -- play the sound only once
end

function diagonal_jump_sensor:on_activated()

  -- We don't use a jumper here because we don't want the delay.
  sol.audio.play_sound("jump")
  hero:start_jumping(5, 56, true)
end

