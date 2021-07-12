{ fetchurl }:
let
fetchNuget = {name, version, sha256}:
  fetchurl {
    inherit sha256;
    url = "https://www.nuget.org/api/v2/package/${name}/${version}";
  };
in [
(fetchNuget {
  name = "ini-parser-netstandard";
  version = "2.5.2";
  sha256 = "14alsxh7ik07xl9xab8bdi108f4xhz8vcchxvxy1k5w3zf3gdml9";
})
(fetchNuget {
  name = "libsodium";
  version = "1.0.18";
  sha256 = "15qzl5k31yaaapqlijr336lh4lzz1qqxlimgxy8fdyig8jdmgszn";
})
(fetchNuget {
  name = "Microsoft.CodeCoverage";
  version = "16.7.0";
  sha256 = "10f6y1q8w61vc8ffqd7jsndwfskkfqbdzfqswyxnrr0qkkqx29v1";
})
(fetchNuget {
  name = "Microsoft.DotNet.InternalAbstractions";
  version = "1.0.0";
  sha256 = "0mp8ihqlb7fsa789frjzidrfjc1lrhk88qp3xm5qvr7vf4wy4z8x";
})
(fetchNuget {
  name = "Microsoft.NETCore.Platforms";
  version = "1.0.1";
  sha256 = "01al6cfxp68dscl15z7rxfw9zvhm64dncsw09a1vmdkacsa2v6lr";
})
(fetchNuget {
  name = "Microsoft.NETCore.Platforms";
  version = "1.1.0";
  sha256 = "08vh1r12g6ykjygq5d3vq09zylgb84l63k49jc4v8faw9g93iqqm";
})
(fetchNuget {
  name = "Microsoft.NETCore.Targets";
  version = "1.1.0";
  sha256 = "193xwf33fbm0ni3idxzbr5fdq3i2dlfgihsac9jj7whj0gd902nh";
})
(fetchNuget {
  name = "Microsoft.NET.Test.Sdk";
  version = "16.7.0";
  sha256 = "1vkp6b82566z2pxn9035wrh4339kz3ki17g5qlwmwdbn4br6lcfy";
})
(fetchNuget {
  name = "Microsoft.TestPlatform.ObjectModel";
  version = "16.7.0";
  sha256 = "0nmw80ap2rn9h4i1x7qb15n763sh3wy8hjp1i5n0av7100g0yjqz";
})
(fetchNuget {
  name = "Microsoft.TestPlatform.TestHost";
  version = "16.7.0";
  sha256 = "0485nv0wcwdwjhif5a7d1i0znaf9acqyawhpqcwschw827chqzrs";
})
(fetchNuget {
  name = "Microsoft.Win32.Primitives";
  version = "4.3.0";
  sha256 = "0j0c1wj4ndj21zsgivsc24whiya605603kxrbiw6wkfdync464wq";
})
(fetchNuget {
  name = "Microsoft.Win32.Registry";
  version = "4.3.0";
  sha256 = "1gxyzxam8163vk1kb6xzxjj4iwspjsz9zhgn1w9rjzciphaz0ig7";
})
(fetchNuget {
  name = "NETStandard.Library";
  version = "2.0.0";
  sha256 = "1bc4ba8ahgk15m8k4nd7x406nhi0kwqzbgjk2dmw52ss553xz7iy";
})
(fetchNuget {
  name = "Newtonsoft.Json";
  version = "12.0.3";
  sha256 = "17dzl305d835mzign8r15vkmav2hq8l6g7942dfjpnzr17wwl89x";
})
(fetchNuget {
  name = "NSec.Cryptography";
  version = "20.2.0";
  sha256 = "19slji51v8s8i4836nqqg7qb3i3p4ahqahz0fbb3gwpp67pn6izx";
})
(fetchNuget {
  name = "NuGet.Frameworks";
  version = "5.0.0";
  sha256 = "18ijvmj13cwjdrrm52c8fpq021531zaz4mj4b4zapxaqzzxf2qjr";
})
(fetchNuget {
  name = "nunit";
  version = "3.12.0";
  sha256 = "1880j2xwavi8f28vxan3hyvdnph4nlh5sbmh285s4lc9l0b7bdk2";
})
(fetchNuget {
  name = "NUnit3TestAdapter";
  version = "3.17.0";
  sha256 = "0kxc6z3b8ccdrcyqz88jm5yh5ch9nbg303v67q8sp5hhs8rl8nk6";
})
(fetchNuget {
  name = "runtime.native.System";
  version = "4.3.0";
  sha256 = "15hgf6zaq9b8br2wi1i3x0zvmk410nlmsmva9p0bbg73v6hml5k4";
})
(fetchNuget {
  name = "System.AppContext";
  version = "4.1.0";
  sha256 = "0fv3cma1jp4vgj7a8hqc9n7hr1f1kjp541s6z0q1r6nazb4iz9mz";
})
(fetchNuget {
  name = "System.Collections";
  version = "4.3.0";
  sha256 = "19r4y64dqyrq6k4706dnyhhw7fs24kpp3awak7whzss39dakpxk9";
})
(fetchNuget {
  name = "System.Collections.NonGeneric";
  version = "4.3.0";
  sha256 = "07q3k0hf3mrcjzwj8fwk6gv3n51cb513w4mgkfxzm3i37sc9kz7k";
})
(fetchNuget {
  name = "System.Collections.Specialized";
  version = "4.3.0";
  sha256 = "1sdwkma4f6j85m3dpb53v9vcgd0zyc9jb33f8g63byvijcj39n20";
})
(fetchNuget {
  name = "System.ComponentModel";
  version = "4.3.0";
  sha256 = "0986b10ww3nshy30x9sjyzm0jx339dkjxjj3401r3q0f6fx2wkcb";
})
(fetchNuget {
  name = "System.ComponentModel.EventBasedAsync";
  version = "4.3.0";
  sha256 = "1rv9bkb8yyhqqqrx6x95njv6mdxlbvv527b44mrd93g8fmgkifl7";
})
(fetchNuget {
  name = "System.ComponentModel.Primitives";
  version = "4.3.0";
  sha256 = "1svfmcmgs0w0z9xdw2f2ps05rdxmkxxhf0l17xk9l1l8xfahkqr0";
})
(fetchNuget {
  name = "System.ComponentModel.TypeConverter";
  version = "4.3.0";
  sha256 = "17ng0p7v3nbrg3kycz10aqrrlw4lz9hzhws09pfh8gkwicyy481x";
})
(fetchNuget {
  name = "System.Diagnostics.Debug";
  version = "4.3.0";
  sha256 = "00yjlf19wjydyr6cfviaph3vsjzg3d5nvnya26i2fvfg53sknh3y";
})
(fetchNuget {
  name = "System.Diagnostics.Process";
  version = "4.3.0";
  sha256 = "0g4prsbkygq8m21naqmcp70f24a1ksyix3dihb1r1f71lpi3cfj7";
})
(fetchNuget {
  name = "System.Globalization";
  version = "4.3.0";
  sha256 = "1cp68vv683n6ic2zqh2s1fn4c2sd87g5hpp6l4d4nj4536jz98ki";
})
(fetchNuget {
  name = "System.Globalization.Extensions";
  version = "4.3.0";
  sha256 = "02a5zfxavhv3jd437bsncbhd2fp1zv4gxzakp1an9l6kdq1mcqls";
})
(fetchNuget {
  name = "System.IO";
  version = "4.3.0";
  sha256 = "05l9qdrzhm4s5dixmx68kxwif4l99ll5gqmh7rqgw554fx0agv5f";
})
(fetchNuget {
  name = "System.IO.FileSystem";
  version = "4.3.0";
  sha256 = "0z2dfrbra9i6y16mm9v1v6k47f0fm617vlb7s5iybjjsz6g1ilmw";
})
(fetchNuget {
  name = "System.IO.FileSystem.Primitives";
  version = "4.3.0";
  sha256 = "0j6ndgglcf4brg2lz4wzsh1av1gh8xrzdsn9f0yznskhqn1xzj9c";
})
(fetchNuget {
  name = "System.Linq";
  version = "4.3.0";
  sha256 = "1w0gmba695rbr80l1k2h4mrwzbzsyfl2z4klmpbsvsg5pm4a56s7";
})
(fetchNuget {
  name = "System.Reflection";
  version = "4.3.0";
  sha256 = "0xl55k0mw8cd8ra6dxzh974nxif58s3k1rjv1vbd7gjbjr39j11m";
})
(fetchNuget {
  name = "System.Reflection.Extensions";
  version = "4.3.0";
  sha256 = "02bly8bdc98gs22lqsfx9xicblszr2yan7v2mmw3g7hy6miq5hwq";
})
(fetchNuget {
  name = "System.Reflection.Primitives";
  version = "4.3.0";
  sha256 = "04xqa33bld78yv5r93a8n76shvc8wwcdgr1qvvjh959g3rc31276";
})
(fetchNuget {
  name = "System.Reflection.TypeExtensions";
  version = "4.3.0";
  sha256 = "0y2ssg08d817p0vdag98vn238gyrrynjdj4181hdg780sif3ykp1";
})
(fetchNuget {
  name = "System.Resources.ResourceManager";
  version = "4.3.0";
  sha256 = "0sjqlzsryb0mg4y4xzf35xi523s4is4hz9q4qgdvlvgivl7qxn49";
})
(fetchNuget {
  name = "System.Runtime";
  version = "4.3.0";
  sha256 = "066ixvgbf2c929kgknshcxqj6539ax7b9m570cp8n179cpfkapz7";
})
(fetchNuget {
  name = "System.Runtime.CompilerServices.Unsafe";
  version = "4.7.0";
  sha256 = "16r6sn4czfjk8qhnz7bnqlyiaaszr0ihinb7mq9zzr1wba257r54";
})
(fetchNuget {
  name = "System.Runtime.Extensions";
  version = "4.3.0";
  sha256 = "1ykp3dnhwvm48nap8q23893hagf665k0kn3cbgsqpwzbijdcgc60";
})
(fetchNuget {
  name = "System.Runtime.Handles";
  version = "4.3.0";
  sha256 = "0sw2gfj2xr7sw9qjn0j3l9yw07x73lcs97p8xfc9w1x9h5g5m7i8";
})
(fetchNuget {
  name = "System.Runtime.InteropServices";
  version = "4.3.0";
  sha256 = "00hywrn4g7hva1b2qri2s6rabzwgxnbpw9zfxmz28z09cpwwgh7j";
})
(fetchNuget {
  name = "System.Runtime.InteropServices.RuntimeInformation";
  version = "4.3.0";
  sha256 = "0q18r1sh4vn7bvqgd6dmqlw5v28flbpj349mkdish2vjyvmnb2ii";
})
(fetchNuget {
  name = "System.Text.Encoding";
  version = "4.3.0";
  sha256 = "1f04lkir4iladpp51sdgmis9dj4y8v08cka0mbmsy0frc9a4gjqr";
})
(fetchNuget {
  name = "System.Text.Encoding.Extensions";
  version = "4.3.0";
  sha256 = "11q1y8hh5hrp5a3kw25cb6l00v5l5dvirkz8jr3sq00h1xgcgrxy";
})
(fetchNuget {
  name = "System.Text.RegularExpressions";
  version = "4.3.0";
  sha256 = "1bgq51k7fwld0njylfn7qc5fmwrk2137gdq7djqdsw347paa9c2l";
})
(fetchNuget {
  name = "System.Threading";
  version = "4.3.0";
  sha256 = "0rw9wfamvhayp5zh3j7p1yfmx9b5khbf4q50d8k5rk993rskfd34";
})
(fetchNuget {
  name = "System.Threading.Tasks";
  version = "4.3.0";
  sha256 = "134z3v9abw3a6jsw17xl3f6hqjpak5l682k2vz39spj4kmydg6k7";
})
(fetchNuget {
  name = "System.Threading.Tasks.Extensions";
  version = "4.3.0";
  sha256 = "1xxcx2xh8jin360yjwm4x4cf5y3a2bwpn2ygkfkwkicz7zk50s2z";
})
(fetchNuget {
  name = "System.Threading.Thread";
  version = "4.3.0";
  sha256 = "0y2xiwdfcph7znm2ysxanrhbqqss6a3shi1z3c779pj2s523mjx4";
})
(fetchNuget {
  name = "System.Threading.ThreadPool";
  version = "4.3.0";
  sha256 = "027s1f4sbx0y1xqw2irqn6x161lzj8qwvnh2gn78ciiczdv10vf1";
})
(fetchNuget {
  name = "System.Xml.ReaderWriter";
  version = "4.3.0";
  sha256 = "0c47yllxifzmh8gq6rq6l36zzvw4kjvlszkqa9wq3fr59n0hl3s1";
})
(fetchNuget {
  name = "System.Xml.XmlDocument";
  version = "4.3.0";
  sha256 = "0bmz1l06dihx52jxjr22dyv5mxv6pj4852lx68grjm7bivhrbfwi";
})
(fetchNuget {
  name = "System.Xml.XPath";
  version = "4.3.0";
  sha256 = "1cv2m0p70774a0sd1zxc8fm8jk3i5zk2bla3riqvi8gsm0r4kpci";
})
(fetchNuget {
  name = "System.Xml.XPath.XmlDocument";
  version = "4.3.0";
  sha256 = "1h9lh7qkp0lff33z847sdfjj8yaz98ylbnkbxlnsbflhj9xyfqrm";
})
]