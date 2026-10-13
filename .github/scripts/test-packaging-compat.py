"""Verify the actual MSBuild import and metadata without building Windows code."""
import json
import os
from pathlib import Path
import subprocess
import tempfile

targets = Path(__file__).with_name('packaging-compat.targets').resolve()
env = dict(os.environ, CustomAfterMicrosoftCommonTargets=str(targets),
           DOTNET_NOLOGO='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
dotnet = os.environ.get('PACKAGING_TEST_DOTNET', 'dotnet')
cases = [
    ('System.Drawing.Common', '*', 'all', '[10.0.12]'),
    ('System.Drawing.Common', '10.0.10', 'all', '10.0.10'),
    ('System.Drawing.Common', '*', '', '*'),
    ('Newtonsoft.Json', '*', 'all', '*'),
]
with tempfile.TemporaryDirectory(prefix='pins-packaging-compat-') as directory:
    root = Path(directory)
    for index, (name, version, exclude, expected) in enumerate(cases):
        project = root / f'case{index}.csproj'
        project.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup>'
            f'<PackageReference Include="{name}" Version="{version}" ExcludeAssets="{exclude}" />'
            '</ItemGroup></Project>', encoding='utf-8')
        result = subprocess.check_output(
            [dotnet, 'msbuild', str(project), '-nologo', '-getItem:PackageReference'],
            env=env, text=True, encoding='utf-8')
        items = json.loads(result)['Items']['PackageReference']
        assert len(items) == 1, items
        assert items[0]['Identity'] == name, items
        assert items[0]['Version'] == expected, items
        assert items[0].get('ExcludeAssets', '') == exclude, items
    subprocess.run([dotnet, 'restore', str(root / 'case0.csproj')], env=env, check=True)
    assets = json.loads((root / 'obj/project.assets.json').read_text(encoding='utf-8'))
    for name in ('System.Drawing.Common', 'Microsoft.Win32.SystemEvents'):
        assert f'{name}/10.0.12' in assets['libraries'], assets['libraries'].keys()
print('Packaging compatibility pin: four MSBuild cases and real NuGet restore passed.')
