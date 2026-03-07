### DocsHopper

Automated and deployment-ready documentation for Grasshopper plugins.

DocsHopper is a utility plugin that extracts component metadata directly from your loaded Grasshopper assemblies and generates static documentation websites. Whether you need a lightweight vanilla HTML page or a fully customized MkDocs Material site deployed to GitHub Pages, DocsHopper handles the entire pipeline within the Grasshopper canvas.

### Key Features

- Metadata Extraction: Automatically reads component names, categories, descriptions, and inputs/outputs from any loaded .gha assembly.
- Multiple Export Formats: Export your documentation as zero-dependency HTML/CSS, structured Markdown, raw JSON, or a complete MkDocs site.
- Advanced Customization: Inject custom CSS, MkDocs YAML configurations, and build rich homepage sections with text and screenshots natively in Grasshopper.
- Live Previewing: Spin up local Python servers for MkDocs or instantly open HTML exports in your default browser.
- One-Click Deployment: Automatically generate GitHub Actions .yml files to push your documentation straight to GitHub Pages.
- Embedded Templates: Right-click the Templates component to instantly drop pre-wired documentation setups onto your canvas.

### Installation

DocsHopper is available via the Rhino Package Manager (YAK):
- Open Rhino.
- Type _PackageManager in the command line.
- Search for DocsHopper.
- Click Install and restart Rhino.
- Alternatively, download the latest .gha from the Releases page and place it in your Grasshopper Libraries folder (%appdata%\Grasshopper\Libraries).

### The Component Suite

DocsHopper is organized into a clean, logical pipeline:

1. Extract & Utilize (Util)
- Plugin Reader: Scans active assemblies and retrieves all available plugin information.
- Templates: Right-click to drop complete, pre-wired DocsHopper workflows directly onto the canvas.

2. Customize (Customize)
- Custom MkDocs: Generate strict YAML configurations (Primary/Accent colors, Dark/Light mode, Repo URLs) for the MkDocs Material theme.
- Custom CSS: Generate custom stylesheets for HTML exports.
- Homepage Section: Author text and attach local image paths to build rich, multi-section homepages for your documentation.

3. Export (Export)
- Export HTML/CSS: Generates a lightweight, standalone static website.
- Export MkDocs: Generates a complete MkDocs directory structure.
- Export Advanced (HTML/MkDocs): Accepts custom CSS/YAML and merged Homepage Sections for deep customization.
- Export JSON / Docs: Raw data serialization for custom downstream processing.

4. Preview & Deploy (Preview / Deploy)
- Preview HTML / MkDocs: Instantly view your generated sites locally.
- Deploy MkDocs: Generates the necessary GitHub Actions workflows for automated web hosting.

### Quick Start Guide
- Drop the Templates component onto your canvas.
- Right-click it and select Basic MkDocs Site.
- Plug the exact name of your target plugin (e.g., "Kangaroo2") into the Plugin Name input.
- Define an Output Directory on your local machine.
- Toggle Run Export to True.
- Wire the output folder into the Preview MkDocs component and toggle Launch Server to view your new documentation.
