// Copyright (c) .NET Foundation and Contributors.  All Rights Reserved.  See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;

using TorchSharp;
using static TorchSharp.torch;

namespace TorchSharp
{
    using Modules;

    namespace Modules
    {
        public class Dirichlet : torch.distributions.ExponentialFamily
        {

            public override Tensor mean => concentration / concentration.sum(-1, true);

            public override Tensor variance {
                get {
                    var con0 = concentration.sum(-1, true);
                    return concentration * (con0 - concentration) / (con0.pow(2) * (con0 + 1));
                }
            }

            public Dirichlet(Tensor concentration)
            {
                var cshape = concentration.shape;
                this.batch_shape = cshape.Take(cshape.Length - 1).ToArray();
                this.event_shape = new long[] { cshape[cshape.Length - 1]};
                this.concentration = concentration;
            }

            internal Tensor concentration;

            public override Tensor rsample(params long[] sample_shape)
            {
                var shape = ExtendedShape(sample_shape);
                var con = concentration.expand(shape);
                return torch._sample_dirichlet(con);
            }

            public override Tensor log_prob(Tensor value)
            {
                return (concentration - 1).xlogy(value).sum(-1) + torch.lgamma(concentration.sum(-1)) - torch.lgamma(concentration).sum(-1);
            }

            public override Tensor entropy()
            {
                var k = concentration.size(-1);
                var a0 = concentration.sum(-1);

                return torch.lgamma(concentration).sum(-1) - torch.lgamma(a0) - (k - a0) * torch.digamma(a0) - ((concentration - 1.0) * torch.digamma(concentration)).sum(-1);
            }

            public override distributions.Distribution expand(long[] batch_shape, distributions.Distribution instance = null)
            {
                if (instance != null && !(instance is Dirichlet))
                    throw new ArgumentException("expand(): 'instance' must be a Dirichlet distribution");

                var shape = new List<long>();
                shape.AddRange(batch_shape);
                shape.AddRange(event_shape);

                var c = concentration.expand(shape.ToArray());

                var newDistribution = ((instance == null) ? new Dirichlet(c) : instance) as Dirichlet;

                newDistribution.batch_shape = batch_shape;
                if (newDistribution == instance) {
                    newDistribution.concentration = c;
                }
                return newDistribution;
            }

            protected override IList<Tensor> NaturalParams => new Tensor[] { concentration - 1 };

            protected override Tensor MeanCarrierMeasure => throw new NotImplementedException();

            protected override Tensor LogNormalizer(params Tensor[] parameters)
            {
                var x = parameters[0];

                return x.lgamma().sum(-1) - x.sum(-1).lgamma();
            }
        }

    }

    public static partial class torch
    {
        public static partial class distributions
        {
            /// <summary>
            /// Creates a Dirichlet distribution parameterized by shape `concentration` and `rate`.
            /// </summary>
            /// <param name="concentration">Shape parameter of the distribution (often referred to as 'α')</param>
            public static Dirichlet Dirichlet(Tensor concentration)
            {
                return new Dirichlet(concentration);
            }
        }
    }
}