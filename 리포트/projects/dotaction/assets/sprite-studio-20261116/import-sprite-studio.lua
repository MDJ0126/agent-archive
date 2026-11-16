local file = assert(io.open(app.params.input, "r"))
local doc = json.decode(file:read("*a"))
file:close()
local sprite = Sprite(doc.width, doc.height, ColorMode.RGB)
local layer = sprite.layers[1]
layer.name = "art"
local palette = Palette(#doc.palette)
for i, rgb in ipairs(doc.palette) do
  palette:setColor(i - 1, Color{r=rgb[1], g=rgb[2], b=rgb[3], a=255})
end
sprite:setPalette(palette)
local start = 1
for i, frame in ipairs(doc.frames) do
  if i > 1 then sprite:newEmptyFrame() end
  sprite.frames[i].duration = frame.duration / 1000
  local image = Image(doc.width, doc.height, ColorMode.RGB)
  for position, color in ipairs(frame.pixels) do
    if color >= 0 then
      local rgb = doc.palette[color + 1]
      image:drawPixel((position - 1) % doc.width, math.floor((position - 1) / doc.width), app.pixelColor.rgba(rgb[1], rgb[2], rgb[3], 255))
    end
  end
  sprite:newCel(layer, sprite.frames[i], image, Point(0, 0))
  if i == #doc.frames or doc.frames[i + 1].tag ~= frame.tag then
    local tag = sprite:newTag(start, i)
    tag.name = frame.tag
    start = i + 1
  end
end
sprite:saveAs(app.params.output)
sprite:close()
-- Reopen the native source and verify persisted frame durations and count.
local reopened = app.open(app.params.output)
assert(#reopened.frames == #doc.frames, "frame count mismatch")
for i, frame in ipairs(doc.frames) do
  assert(math.abs(reopened.frames[i].duration * 1000 - frame.duration) < 1, "duration mismatch")
end
reopened:close()
