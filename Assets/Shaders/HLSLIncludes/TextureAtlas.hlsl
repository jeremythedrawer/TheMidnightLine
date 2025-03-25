float HorizontalPos(float uvX, float atlasSizeX, float index)
{
	float xPos = fmod(index, atlasSizeX);
	xPos = floor(xPos);
	xPos = xPos / atlasSizeX;

    float uvXScale = uvX / atlasSizeX;
	
	xPos = xPos + uvXScale;
	return xPos;
}

float VerticalPos(float uvY, float2 atlasSize, float index)
{
    float minAxisLength = min(atlasSize.x, atlasSize.y);
    float yPos = floor(index / minAxisLength);
	yPos = atlasSize.y - yPos - 1;
	yPos = yPos / atlasSize.y;
	
    float uvYScale = uvY / atlasSize.y;
	
	yPos = yPos + uvYScale;
	
	return yPos;
}

float2 TextureAtlasUV (float2 uv, float2 atlasSize, float index, float scale)
{
    float maxSize = max(atlasSize.x, atlasSize.y);
    uv *= scale;
    uv = abs(fmod(uv, 1 / maxSize));
	float xPos = HorizontalPos(uv.x, atlasSize.x, index);
	float yPos = VerticalPos(uv.y, atlasSize, index);
    return float2(xPos, yPos);
}