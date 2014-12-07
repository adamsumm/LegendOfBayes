local map = ...
-- Cave of the hole near the flower

function map:on_started(destination)

  if destination ~= main_entrance then
    map:set_doors_open("door", true)
  end
end

function close_door_sensor:on_activated()

  if door:is_open() then
    map:close_doors("door")
  end
end

