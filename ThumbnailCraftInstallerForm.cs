using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal enum ThumbnailCraftInstallerMode
{
    Install,
    AlreadyInstalled
}

internal sealed class ThumbnailCraftInstallerForm : Form
{
    private const string PreviewResource = "ThumbnailCraftIcon-preview.png";
    private static readonly Color Accent = Color.FromArgb(35, 164, 105);
    private static readonly Color AccentDark = Color.FromArgb(22, 125, 78);
    private static readonly Color Ink = Color.FromArgb(24, 35, 32);
    private static readonly Color Muted = Color.FromArgb(91, 105, 99);
    private static readonly Color Surface = Color.FromArgb(247, 250, 248);

    private readonly ThumbnailCraftInstallerMode mode;
    private readonly Panel contentPanel;
    private readonly Label titleLabel;
    private readonly Label descriptionLabel;
    private readonly Label statusLabel;
    private readonly Label contactLabel;
    private readonly Button primaryButton;
    private readonly Button exitButton;
    private readonly Button closeButton;
    private readonly ProgressBar progressBar;
    private readonly Label featureOne;
    private readonly Label featureTwo;
    private bool installing;

    public ThumbnailCraftInstallerForm(ThumbnailCraftInstallerMode mode)
    {
        this.mode = mode;
        Text = "Thumbnail Craft";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(780, 460);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        BackColor = Color.White;
        ShowInTaskbar = true;
        DoubleBuffered = true;

        Panel brandPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(292, 460),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(13, 31, 25)
        };
        brandPanel.Paint += PaintBrandPanel;

        PictureBox logo = new PictureBox
        {
            Location = new Point(47, 47),
            Size = new Size(198, 198),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = LoadPreviewImage()
        };
        brandPanel.Controls.Add(logo);

        Label brand = CreateLabel("THUMBNAIL", new Font("Segoe UI", 20F, FontStyle.Bold), Color.White);
        brand.Location = new Point(43, 270);
        brand.Size = new Size(230, 32);
        brandPanel.Controls.Add(brand);

        Label brandCraft = CreateLabel("CRAFT", new Font("Segoe UI", 20F, FontStyle.Bold), Color.White);
        brandCraft.Location = new Point(43, 308);
        brandCraft.Size = new Size(230, 32);
        brandPanel.Controls.Add(brandCraft);

        Label brandSubline = CreateLabel("PCraft + VectorCraft + PDF", new Font("Segoe UI", 9.5F, FontStyle.Regular), Color.FromArgb(173, 208, 190));
        brandSubline.Location = new Point(46, 350);
        brandSubline.Size = new Size(220, 24);
        brandPanel.Controls.Add(brandSubline);

        Label version = CreateLabel("ZIGOVO  ·  PRIVATE TOOL", new Font("Segoe UI", 8F, FontStyle.Bold), Color.FromArgb(103, 158, 132));
        version.Location = new Point(46, 414);
        version.Size = new Size(220, 18);
        brandPanel.Controls.Add(version);

        contentPanel = new Panel
        {
            Location = new Point(292, 0),
            Size = new Size(488, 460),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Surface,
            Padding = new Padding(0)
        };
        Controls.Add(brandPanel);
        Controls.Add(contentPanel);

        closeButton = CreateButton("×", Color.Transparent, Muted, new Size(34, 30));
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Font = new Font("Segoe UI", 16F, FontStyle.Regular);
        closeButton.Location = new Point(436, 12);
        closeButton.Click += delegate { Close(); };
        contentPanel.Controls.Add(closeButton);

        Label eyebrow = CreateLabel("THUMBNAIL CRAFT", new Font("Segoe UI", 8.5F, FontStyle.Bold), AccentDark);
        eyebrow.Location = new Point(38, 38);
        eyebrow.Size = new Size(260, 20);
        contentPanel.Controls.Add(eyebrow);

        titleLabel = CreateLabel("Instalar Thumbnail Craft", new Font("Segoe UI", 21F, FontStyle.Bold), Ink);
        titleLabel.Location = new Point(35, 67);
        titleLabel.Size = new Size(390, 42);
        contentPanel.Controls.Add(titleLabel);

        descriptionLabel = CreateLabel(
            "Activa miniaturas para tus archivos .pcraft, .vectorcraft y PDF en el Explorador de Windows.",
            new Font("Segoe UI", 10.5F, FontStyle.Regular), Muted);
        descriptionLabel.Location = new Point(38, 122);
        descriptionLabel.Size = new Size(382, 48);
        contentPanel.Controls.Add(descriptionLabel);

        Panel featureCard = new Panel
        {
            Location = new Point(38, 184),
            Size = new Size(382, 96),
            BackColor = Color.White
        };
        featureCard.Paint += PaintFeatureCard;
        contentPanel.Controls.Add(featureCard);

        featureOne = CreateLabel("✓   Vista previa de documentos en Explorer", new Font("Segoe UI", 9.5F, FontStyle.Regular), Ink);
        featureOne.Location = new Point(18, 17);
        featureOne.Size = new Size(340, 24);
        featureCard.Controls.Add(featureOne);

        featureTwo = CreateLabel("✓   Registro automático para .pcraft, .vectorcraft y PDF", new Font("Segoe UI", 9.5F, FontStyle.Regular), Ink);
        featureTwo.Location = new Point(18, 53);
        featureTwo.Size = new Size(340, 24);
        featureCard.Controls.Add(featureTwo);

        statusLabel = CreateLabel("", new Font("Segoe UI", 10F, FontStyle.Bold), AccentDark);
        statusLabel.Location = new Point(38, 292);
        statusLabel.Size = new Size(382, 28);
        statusLabel.Visible = false;
        contentPanel.Controls.Add(statusLabel);

        progressBar = new ProgressBar
        {
            Location = new Point(38, 323),
            Size = new Size(382, 5),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Visible = false
        };
        contentPanel.Controls.Add(progressBar);

        contactLabel = CreateLabel("Contacto: benny@zigovo.com", new Font("Segoe UI", 8.5F, FontStyle.Regular), Muted);
        contactLabel.Location = new Point(38, 374);
        contactLabel.Size = new Size(180, 20);
        contentPanel.Controls.Add(contactLabel);

        primaryButton = CreateButton("Instalar", Accent, Color.White, new Size(142, 40));
        primaryButton.Location = new Point(223, 364);
        primaryButton.Click += PrimaryButtonClick;
        contentPanel.Controls.Add(primaryButton);

        exitButton = CreateButton("Salir", Color.White, Ink, new Size(88, 40));
        exitButton.FlatAppearance.BorderColor = Color.FromArgb(208, 219, 213);
        exitButton.FlatAppearance.BorderSize = 1;
        exitButton.Location = new Point(373, 364);
        exitButton.Click += delegate { Close(); };
        contentPanel.Controls.Add(exitButton);

        if (mode == ThumbnailCraftInstallerMode.AlreadyInstalled)
            ConfigureAlreadyInstalled();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (installing) e.Cancel = true;
        base.OnFormClosing(e);
    }

    private void ConfigureAlreadyInstalled()
    {
        titleLabel.Text = "Ya está instalado";
        descriptionLabel.Text = "Thumbnail Craft ya está instalado y funcionando. Se actualizó la caché de Explorer.";
        statusLabel.Text = "✓  Caché de Explorer actualizada";
        statusLabel.ForeColor = AccentDark;
        statusLabel.Visible = true;
        progressBar.Visible = false;
        featureOne.Text = "✓   El proveedor de miniaturas está activo";
        featureTwo.Text = "✓   .pcraft, .vectorcraft y PDF configurados";
        primaryButton.Text = "Cerrar";
        primaryButton.Location = new Point(223, 364);
        primaryButton.Click -= PrimaryButtonClick;
        primaryButton.Click += delegate { Close(); };
        exitButton.Visible = false;
    }

    private void PrimaryButtonClick(object sender, EventArgs e)
    {
        if (installing) return;
        installing = true;
        primaryButton.Enabled = false;
        exitButton.Enabled = false;
        closeButton.Enabled = false;
        titleLabel.Text = "Instalando...";
        descriptionLabel.Text = "Configurando el proveedor y actualizando Explorer.";
        statusLabel.Text = "Preparando Thumbnail Craft";
        statusLabel.ForeColor = AccentDark;
        statusLabel.Visible = true;
        progressBar.Visible = true;

        Thread worker = new Thread(new ThreadStart(delegate
        {
            try
            {
                PhotoCraftThumbnailSetup.Install(true);
                BeginInvoke((Action)SetSuccess);
            }
            catch (Exception ex)
            {
                BeginInvoke((Action)delegate { SetError(ex.Message); });
            }
        }));
        worker.IsBackground = true;
        worker.Start();
    }

    private void SetSuccess()
    {
        installing = false;
        progressBar.Visible = false;
        titleLabel.Text = "Instalación completa";
        descriptionLabel.Text = "Thumbnail Craft ya está listo. Abre una carpeta y usa iconos grandes para ver tus miniaturas.";
        statusLabel.Text = "✓  Instalado correctamente";
        statusLabel.ForeColor = AccentDark;
        statusLabel.Visible = true;
        featureOne.Text = "✓   Explorer fue actualizado";
        featureTwo.Text = "✓   .pcraft, .vectorcraft y PDF activos";
        primaryButton.Text = "Cerrar";
        primaryButton.Enabled = true;
        primaryButton.Click -= PrimaryButtonClick;
        primaryButton.Click += delegate { Close(); };
        exitButton.Visible = false;
        closeButton.Enabled = true;
    }

    private void SetError(string message)
    {
        installing = false;
        progressBar.Visible = false;
        titleLabel.Text = "No se pudo instalar";
        descriptionLabel.Text = message;
        statusLabel.Text = "!  Revisa los permisos e inténtalo de nuevo";
        statusLabel.ForeColor = Color.FromArgb(190, 65, 57);
        statusLabel.Visible = true;
        primaryButton.Text = "Cerrar";
        primaryButton.Enabled = true;
        primaryButton.Click -= PrimaryButtonClick;
        primaryButton.Click += delegate { Close(); };
        exitButton.Visible = false;
        closeButton.Enabled = true;
    }

    private static Label CreateLabel(string text, Font font, Color color)
    {
        return new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor, Size size)
    {
        Button button = new Button
        {
            Text = text,
            Size = size,
            BackColor = backColor,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            TabStop = true,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 181, 119);
        button.FlatAppearance.MouseDownBackColor = AccentDark;
        return button;
    }

    private static Image LoadPreviewImage()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream stream = assembly.GetManifestResourceStream(PreviewResource))
        {
            if (stream == null) return null;
            using (Image image = Image.FromStream(stream)) return new Bitmap(image);
        }
    }

    private void PaintBrandPanel(object sender, PaintEventArgs e)
    {
        Panel panel = (Panel)sender;
        using (LinearGradientBrush brush = new LinearGradientBrush(panel.ClientRectangle,
            Color.FromArgb(13, 31, 25), Color.FromArgb(18, 62, 45), 315F))
        {
            e.Graphics.FillRectangle(brush, panel.ClientRectangle);
        }
        using (Pen pen = new Pen(Color.FromArgb(33, 117, 82), 1F))
        {
            e.Graphics.DrawLine(pen, 20, 26, 272, 26);
            e.Graphics.DrawLine(pen, 20, 432, 272, 432);
        }
    }

    private static void PaintFeatureCard(object sender, PaintEventArgs e)
    {
        Panel panel = (Panel)sender;
        using (Pen pen = new Pen(Color.FromArgb(222, 231, 226), 1F))
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        }
    }
}
