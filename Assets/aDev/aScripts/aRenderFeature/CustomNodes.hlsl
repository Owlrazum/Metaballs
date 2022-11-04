void DetermineBlobColor_float(
    float InputAlpha,
    float OuterInnerThreshold, 
    float InnerHighlightThreshold, 
    float4 EmptyColor,
    float4 OuterBlobColor,
    float4 InnerBlobColor,
    float4 HighlightColor,
    out float4 Color
)
{
    if (InputAlpha > 0)
    {
        if (InputAlpha > OuterInnerThreshold)
        {
            if (InputAlpha > InnerHighlightThreshold)
            {
                Color = HighlightColor;
            }   
            else
            {
                Color = InnerBlobColor;
            }
        }
        else
        {
            Color = OuterBlobColor;
        }
    }
    else
    {
        Color = EmptyColor;
    }
}

void DetermineMetaballAlpha_float(
    float InputRed,
    float GreenThreshold,
    float RedThreshold,
    float OuterMetaballAlpha,
    float InnerMetaballAlpha,
    float HighlightAlpha,
    out float Alpha,
    out float3 BaseColor
)
{
    if (InputRed > RedThreshold)
    {
        Alpha = InnerMetaballAlpha;
        BaseColor = (0, 0, 0);
    }
    else
    {
        Alpha = 0;
        BaseColor = (0, 0, 0);
    }
    
}