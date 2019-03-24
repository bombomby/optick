cbuffer ConstantBuffer : register(b0)
{
	matrix View;
	matrix World;
}

Texture2D FontTexture : register(t0);
SamplerState FontSampler : register(s0);

struct VS_IN
{
	float2 pos : POSITION;
	float2 uv  : TEXCOORD0;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 uv  : TEXCOORD0;
	float4 col : COLOR;
};

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	float4x4 wv = mul(View, World);

	output.pos = mul(wv, float4(input.pos, 0.5f, 1.0f));
	output.uv = input.uv;
	output.col = input.col;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	float4 tex = FontTexture.Sample(FontSampler, input.uv);
	return float4(input.col.rgb, tex.r);// *input.col;
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader(0);
		SetVertexShader(CompileShader(vs_4_0, VS()));
		SetPixelShader(CompileShader(ps_4_0, PS()));
	}
}