#! perl

my @files = `ls Levels/LoZ/*/*.png`;

foreach my $file (@files){
chomp($file);
	system("convert $file -define png:color-type='2' temp.png");
	system("mv temp.png $file");
}