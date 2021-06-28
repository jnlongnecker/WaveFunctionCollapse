If you're reading this, thank you for reading the readme. THIS IS NOT THE OFFICIAL SOURCE CODE, I REPEAT THIS 
IS NOT THE OFFICIAL SOURCE CODE. If you want to find the original source code this is forked from, 
it can be found here: https://github.com/mxgmn/WaveFunctionCollapse. This code, while functionally identical 
to the original Wave Function Collapse algorithm, has my own comments for making sense of the algorithm.

This edit is made to be educational in purposes only, and in no way claims ownership, authorship, or any right
to the algorithm or source code. The only thing original about these files is this readme and the comments 
within the algorithm itself. I hope you find these files helpful in your understanding of this very
interesting and useful algorithm.

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

