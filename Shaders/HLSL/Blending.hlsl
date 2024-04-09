#ifndef __BLENDING__7521P75SKEHFII75H29F__
#define __BLENDING__7521P75SKEHFII75H29F__

// polynomial smooth min (k = 0.1);
float polysmin( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return lerp( b, a, h ) - k*h*(1.0-h);
}

// power smooth min (k = 8);
float powersmin( float a, float b, float k )
{
    a = pow( a, k ); b = pow( b, k );
    return pow( (a*b)/(a+b), 1.0/k );
}

// exponential smooth min (k = 32);
float expsmin( float a, float b, float k )
{
    float res = exp2( -k*a ) + exp2( -k*b );
    return -log2( res )/k;
}

float smax( float a, float b, float k )
{
    float h = max(k-abs(a-b),0.0);  
    return max(a, b) + h*h*0.25/k;
}

// END OF __BLENDING__7521P75SKEHFII75H29F__ 
#endif