If you're reading this, thank you for reading the readme. THIS IS NOT THE OFFICIAL SOURCE CODE, I REPEAT THIS 
IS NOT THE OFFICIAL SOURCE CODE. If you want to find the original source code this is forked from, 
it can be found here: https://github.com/mxgmn/WaveFunctionCollapse. This code, while functionally identical 
to the original Wave Function Collapse algorithm, has my own comments for making sense of the algorithm.

This edit is made to be educational in purposes only, and in no way claims to be the original author.
The only thing original about these files is this readme and the comments within the algorithm itself. I 
hope you find these files helpful in your understanding of this very interesting and useful algorithm.

At this point in time, the only variation of this algorithm I'm particularly interested in is the Overlapping
Model version, so that is the one that is commented. Of course, the base Model class has comments as it's
critical in understanding how the algorithm works at the base level, but essentially if you want to learn 
more about the Simple Tiled Model version, well you're on your own.

If you'd like to read through the code and comments, it should be helpful. However, the breakdown of the
algorithm on the official repo I find to be slightly lacking in the aspect of details necessary to 
program a version of this algorithm, hence the effort in commenting the code and making sense of it.
Here is a (hopefully) more comprehensive and useful breakdown of how the algorithm works using the 
Overlapping Model:

/ ----------------------------------------------------------------------------------------------------------------------------- \
OverlappingModel(string name, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry, int ground)
\ ----------------------------------------------------------------------------------------------------------------------------- /

First off, we're going to need to make sense of this huge amount of inputs:
- name refers to the name of the file you're using as your sample texture. 
- N is the dimensions in pixels for your patterns to be made of. The N value I've seen most commonly used is 3, meaning
    that each pattern is a 3x3 piece of the sample texture given
- width is how many pixels wide the output texture should be
- height is how many pixels high the output texture should be
- periodicInput is whether the input should be periodic. This means that the input texture will be sampled as if it repeats
    in all directions. Usually this should probably be turned on as it will result in more samples being created
- periodicOutput is whether teh output should be periodic. In case you need that clarified, that means that the output will be
    created in such a way that it will wrap; in other words each opposite end will match with each other. This will allow you to
    copy-paste the output as many times as you want and it will match up with itself.
- symmetry is how many degrees of symmetry the patterns should have, which is between 1-8. Interestingly enough, the original 
    code does not do any checking for if the degrees of symmetry is invalid, so if you are just interested in using the
    original algorithm, make sure you're careful about that. Anyways, the degrees of symmetry are just various reflections and
    rotations of the pattern. The more degrees of symmetry, the more patterns are made.
- ground is a strange input that while I understand what it does, the motivation behind it escapes me. If you specify a ground of
    2, for example, the second pattern in the list of patterns will be deleted and in the last row for the output texture,
    every pattern will be deleted. This is how it works for every specification of ground, and in the event that you specify a 
    number that is greater than the number of unique patterns, it just wraps around to the beginning. If you specify a ground
    of 0, however, it does nothing. The default is 0 though so I guess unless you know what you're doing, don't bother with this 
    input.

Whew, now that that is out of the way, let's explain what this constructor does. The TL;DR is it sets up really 1 variable:
the propagator. That's, at least, the most important thing that it does. Let's go into the step-by-step of how it actually
accomplishes this.

1. We do quite a bit of setup. We get the sample and then we find out how many unique colors are in the sample. This number,
which is called C in the algorithm (you'll soon find that the naming conventions here are less than helpful), is used for 
converting a pattern (passed in as an array) into a unique number that describes it, and deconverting this number back into 
an array. The number is referred to as an index which is horribly confusing so I will be referring to this index number and the 
array itself as a pattern instead, as they're interchangable. After that, some helper methods are set up to create these patterns
as well as rotate and reflect them.

2. Now we actually create all of the patterns and their symmetries using the helper methods we just defined. If
a pattern is created multiple times, it gets a weight to it that gets increased. It's important that each pattern only appears
once because of how the selection of patterns occurs so this weight makes it so a pattern that gets created multiple times 
has a higher chance of being selected in the random selection of a pattern.

3. We define an important variable T, which is the number of unique patterns. This number will be used quite
a bit later, so be sure to remember it. We also convert the dynamic data strucutures to more memory and time efficient arrays as
well as define a helper function to tell if two patterns match up nicely on a given side.

4. All this to FINALLY set up the propagator. We do this by looping through each of the 4 cardinal directions. For each unique
pattern, we create a list of patterns that match on the given cardinal direction and then store those as well in the propagator.
For example, the order for 0-3 is LEFT, UP, RIGHT, DOWN, so propagator[0][2] is a list of every pattern that matches up with
pattern 2 on the left side.

And we're finally done. We've used a lot of variables in here, but here are the ones that will actually be used later:
- T: I hate the name for this, but this is the number of unique patterns.
- propagator: A holder for each pattern that matches up with a given pattern on a given side.
- weights: A numerical weight for each unique pattern; also can be thought of as the number of times that unique pattern was
    created.

Again, TL;DR we set up the propagator.

/ ----------------------------------------------------------------------------------------------------------------------------- \
OnBoundary(int x, int y)
\ ----------------------------------------------------------------------------------------------------------------------------- /

This one is actually just one line, and you may get an idea what it does from the name but lets just explain it really quick.
If the output is supposed to be periodic IE looping, then this just returns false. Otherwise, this will return true if the
x,y coordinate given is in a spot on the output texture map that would cause a placement of a pattern there to be outside of the
output texture dimensions. It's there to make sure that we don't wrap if we're not supposed to.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Graphics()
\ ----------------------------------------------------------------------------------------------------------------------------- /

Again, you may be able to figure out what this does from the name, but it's really not very well named so let's explain it. 
What it does is it will create a visualization of the output. If the output is solved, then it gives you the solved texture.
If the output is not finished, it will gives you the average of the remaining patterns for each pixel. Essentially this just 
gives you what you run the algorithm for: the output.



With that, we've finished the OverlappingModel class functionality. We'll talk about Clear when we talk about it in the base
class, so let's move on to Model
/ ----------------------------------------------------------------------------------------------------------------------------- \
Init()
\ ----------------------------------------------------------------------------------------------------------------------------- /

Init is another setup method. It's really just the constructor, but because it has to be run after the child constructor and you
can't do that in C#, they did it like this. It's whatever. This one is a lot less complex than the earlier one and mostly is 
just creating the variables necessary for the algorithm to work. Let's go through it:

1. We set up the existence of 2 variables: wave and compatible. 
    - wave: Tracks for each pixel and pattern whether that pattern has been selected for that pixel. 
    - compatible: Tracks the number of patterns that are a match for each pattern on each pixel.

2. Next we calculate a few values, mostly default values.
    - weightLogWeight: Stores the log of every weight * the weight. This is used as part of deleting (or banning) a pattern.
    - sumOfWeights: As the name implies. This is used as the default value for sumsOfWeights, which is the sum of all the weights
        for the remaining patterns for a given pixel.
    - sumOfWeightLogWeights: The sum of all the log weights. This is used as the default value for sumsOfWeightLogWeights, which
        is the sum of all the weightLogWeight for the remaining patterns for a given pixel.
    - startingEntropy: The default entropy for each pixel. This is calculated from our two weight sums.

3. We finish by creating some more variables.
    - sumsOfOnes: Perhaps the worst named variable, this is the total number of patterns remaining for a given pixel.
    - sumsOfWeights: For each pixel, the sum of all the weights for the remaining patterns.
    - sumsOfWeightLogWeights: For each pixel, the sum of all the weightLogWeights for the remaining patterns.
    - entropies: For each pixel, the entropy of that pixel.
    - stack: A stack data strucutre in order to track banned pixel / pattern pairs.
    - stacksize: The index of the stack head.

Let's finish off by defining this "entropy" thing. The relevant definition for our purposes here is: Entropy is often
interpreted as the degree of disorder or randomness in the system. Entropy will decrease as a pixel (our "system") has 
less patterns remaining to choose from. This will become important as we discuss the next method.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Observe()
\ ----------------------------------------------------------------------------------------------------------------------------- /

If the name of this function makes no sense to you, don't worry that's to be expected. Here's my poor, layman's explanation
of what this function is in reference to: It is referring to quantum mechanics here, specifically in relation to superposition
of quantum particles. While unobserved, quantum particles live in this state of superposition, where there are a multitude of 
possible places that the particle could actually be. It is only when the particle becomes observed that a position is chosen,
this is referred to as a collapse of the wave function to a single final outcome. Hopefully now it has become pretty clear
why this algorithm is called Wave Function Collapse, but in case it's still a little muddy let me explain further by completing
the analogy. In our instance, each pixel is a quantum particle: it lives in this state of uncertainty as it has all these
values it could possible be, these being the patterns. It is only when a particle is observed that we are able to determine
exactly which value it should end up being, and that's exactly what our Observe function models here: it makes a decision
for a certain pixel about which pattern it should be.

That's neat and all, but how do we actually do that? Well, let's dig into it.

1. First off, we need to find which pixel has the lowest entropy. We ignore all pixels that have only 1 pattern remaining, 
    since those are already solved / determined. We want the lowest entropy pixel because that's going to be the one
    closest to being solved, it's the least random choice to make. The method also adds on a slight bit of noise, to be 
    honest I don't know exactly why but it probably just makes it slightly more random.

2. If we didn't select any of the pixels, that means thata all the pixels are solved and we're done making selections.
    We quickly create the observed variable, which is just which pattern each pixel ended up selecting. Then we return
    true to let everyone know the algorithm was a success.

3. If we did make a selection, we then choose a random pattern from the patterns remaining and then ban all the other
    patterns. We'll go into the Ban method next, but it's essentially just a delete button. We then return null to signify
    that we've made a selection for one pixel, but we've neither solved nor contradicted the system.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Ban(int i, int t)
\ ----------------------------------------------------------------------------------------------------------------------------- /

As mentioned earlier, Ban is just a delete button for a pattern (t) at a specific pixel value (i). Here's what changes when
you give a pattern the banhammer:

1. The value for that pattern in wave gets set to false, meaning not a possible choice.

2. All values in compatible for that pattern get set to 0, since it has no adjacent compatible patterns as it's not a possibility.

3. The sums for the input pixel are recalculated to reflect the pattern no longer being an option.

4. The entropy for the input pixel is recalculated.

5. The pixel / pattern pair is pushed to the stack to calculate the propogation effect of this pattern no longer being possible.

And that's all there is to it.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Propagate()
\ ----------------------------------------------------------------------------------------------------------------------------- /

The purpose of this function is to calculate the ripple effect of patterns being banned, as patterns that only fit to that pattern
no longer become possible, and then patterns that depended on that pattern no longer become possible and so on. Here's how it works:

1. Pop a pattern from the stack. For that pixel / pattern pair, check each cardinal direction.

2. Make sure that the pixel is one we need to bother checking by calling OnBoundary (pixels on the boundary have no patterns).
    Again, this only does anything if our output is not periodic. Make sure to wrap the value if it is periodic.

3. Get the patterns that match our banned pattern on the current side and reduce the number of compatible patterns for those 
    patterns.

4. If this causes our non-banned pixel / pattern pair to no longer have any compatible patterns, ban that pixel / pattern pair 
    as well.

5. Repeat until we have nothing on the stack.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Run(int seed, int limit)
\ ----------------------------------------------------------------------------------------------------------------------------- /

This is the generic structure of the actual algorithm itself. It's actually quite simple, so we'll just go over the outline:

1. If wave is null, we know we haven't run the algorithm before so we call our Init() (aka our Model constructor in disguise.)

2. Call clear and seed our random number generator so we can get a consistent output if we want that.

3. We then make Observations and then Propagate those Observations until we either solve the system or reach a contradiction.
    Alternatively, we may decide to set a limit on the number of Observation cycles we can make, and if we reach that then we 
    stop and return true. If limit == 0, there is no limit to Observations we can make.

And that's it, the main thing here is just the cycles of observation and propagation, that's about all that's going on here.

/ ----------------------------------------------------------------------------------------------------------------------------- \
Clear()
\ ----------------------------------------------------------------------------------------------------------------------------- /

Clear just sets up a new run of the algorithm. Here's a list of what gets set up:

- wave gets every value sets to true, since each pattern is possible at the start.
- compatible gets the number of compatible patterns set for each pattern at each pixel.
- sumsOfOnes gets each value set to the default value of the number of weights, aka number of unique patterns. Why they didn't 
    use T here, I really have no idea. Probably has to do with the other version of the algorithm.
- sumsOfWeights gets each value set to the default value of the sum of all weights.
- sumsOfWeightLogWeights gets each value set to the default value of the sum of all weightLogWeights.
- entropies gets each value set to the default value of startingEntropy.

In the OverlappingModel version of this algorithm, this is where the ground parameter comes into play by banning those patterns at
the positions I mentioned previously.



And there you have it, that's the algorithm. To close us out, something to mention is the nature of the code itself. A lot of 
things about this code could be refactored in order to make it easier to understand, but the fact of the matter is is that it
was likely to have been written more sensically to start out with and then refactored into its current state to optimize
performance. After all, it was written to be a library and not really some code that tons of people are meant to read and 
add on to. This is not to say that if you were to write your own version of this algorithm that you shouldn't break things 
apart and use more memory-intesive or less optimal structures that make it easier to understand what's happening but impact
performance, just understand the reasoning behind the current state of the code. You may also find it useful to dig into the 
more complex math manipulating the arrays, there's some pretty clever stuff going on in there.

In the repository and the repository that the source files come from, there are a number of links to this algorithm written 
in a number of languages, so likely there's no need for you to write your own version unless you have a specific need. I hope 
that this document and the comments within the code itself are helpful to your understanding, and good luck in whatever 
project you plan on using this algorithm in!