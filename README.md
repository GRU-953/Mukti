<div align="center">

<img src="./src/Mukti.Mac/wwwroot/mukti-icon.svg" width="96" height="96" alt="Mukti logo" />

# মুক্তি — Mukti

**পুরনো বিজয় বাংলাকে ইউনিকোডে রূপান্তর করুন — সরাসরি Microsoft Office-এ**

*Convert legacy Bijoy Bengali to Unicode — right inside Microsoft Office*

<br/>

[![Windows ডাউনলোড](https://img.shields.io/badge/Windows-ডাউনলোড_করুন-26336E?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/GRU-953/Mukti/releases/latest)
[![Mac ডাউনলোড](https://img.shields.io/badge/Mac-ডাউনলোড_করুন-14A88A?style=for-the-badge&logo=apple&logoColor=white)](https://github.com/GRU-953/Mukti/releases/latest)
[![MIT লাইসেন্স](https://img.shields.io/badge/License-MIT-E0990F?style=for-the-badge)](LICENSE)

<br/>

![Offline](https://img.shields.io/badge/সম্পূর্ণ_অফলাইন-ইন্টারনেট_লাগে_না-26336E?style=flat-square)
![Free](https://img.shields.io/badge/১০০%25_বিনামূল্যে-সবসময়-14A88A?style=flat-square)
![Open Source](https://img.shields.io/badge/Open_Source-সোর্স_কোড_পাবলিক-E0990F?style=flat-square)
![Office](https://img.shields.io/badge/Office_2013_এবং_তার_পরবর্তী-সমর্থিত-E15B3C?style=flat-square)

</div>

---

## মুক্তি কী? (What is Mukti?)

আপনার কাছে পুরনো বিজয় বা সুতোন্নীএমজে ফন্টে টাইপ করা Word, Excel বা PowerPoint ফাইল আছে — কিন্তু সেগুলো আজকের কম্পিউটারে ঠিকমতো দেখায় না? মুক্তি এই সমস্যার সমাধান করে।

You have old documents written in Bijoy or SutonnyMJ Bengali font that no longer display correctly on modern computers. Mukti fixes them — permanently — without leaving Microsoft Office.

> **সহজ ভাষায় / In plain English:** মুক্তি Word, Excel বা PowerPoint-এ একটি নতুন বোতাম যোগ করে। সেই বোতামে ক্লিক করুন, আপনার ফাইল স্ক্যান হবে, এবং পুরনো বাংলা ইউনিকোডে পরিণত হবে — সব কিছু Office-এর ভেতরেই।
>
> Mukti adds one button to your Word, Excel, or PowerPoint ribbon. Click it, scan your document, convert — all from inside Office. Like Grammarly or Quillbot, it works right where your document already is.

---

## ডাউনলোড (Download)

<div align="center">

**[➜ সর্বশেষ সংস্করণ ডাউনলোড করুন — Download Latest Version](https://github.com/GRU-953/Mukti/releases/latest)**

</div>

Releases page খুললে Windows এবং Mac দুটি ফাইল দেখতে পাবেন। আপনার কম্পিউটার অনুযায়ী ডাউনলোড করুন।

On the releases page you will see both a Windows file and a Mac file. Download the one that matches your computer.

| আপনার কম্পিউটার | ডাউনলোড করুন |
|---|---|
| Windows (x64 — সাধারণ) | `Mukti-Setup-x.x.xx.exe` |
| Windows (ARM64 — preview) | `Mukti-Setup-x.x.xx-arm64.exe` |
| Mac (Intel + Apple Silicon) | `Mukti-x.x.xx.pkg` |

---

## ইনস্টল করুন — Windows (How to Install — Windows)

### ধাপ ১: .NET 8 চেক করুন

মুক্তি চালানোর জন্য Microsoft-এর বিনামূল্যে **.NET 8 Desktop Runtime** প্রয়োজন।
ইনস্টলার নিজেই পরীক্ষা করে — যদি না পায়, সে আপনাকে ডাউনলোড করতে সাহায্য করবে।

Mukti needs Microsoft's free .NET 8 Desktop Runtime.
The installer checks automatically — if it's missing, it opens the Microsoft download page for you.

<details>
<summary>▶ .NET 8 নিজে ইনস্টল করতে চাইলে (click to expand)</summary>

1. [**https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) এই লিংকে যান।
2. **".NET 8 Desktop Runtime (x64)"** ডাউনলোড করুন।
3. ডাউনলোড হলে ইনস্টল করুন।
4. তারপর মুক্তির ইনস্টলার আবার চালান।

</details>

---

### ধাপ ২: মুক্তি ইনস্টল করুন

1. ডাউনলোড করা **`Mukti-Setup-x.x.xx.exe`** ফাইলে **ডাবল-ক্লিক** করুন।
   *(Double-click the downloaded installer file.)*

2. ইনস্টলার শুরু হবে। "Install" ক্লিক করুন।
   *(The installer opens. Click Install.)*

3. **কোনো administrator পাসওয়ার্ড লাগবে না** — মুক্তি শুধু আপনার অ্যাকাউন্টের জন্য ইনস্টল হয়।
   *(No administrator password needed — Mukti installs just for your account.)*

4. ইনস্টলার সম্পূর্ণ হলে **Word, Excel বা PowerPoint** খুলুন।
   *(Once done, open Word, Excel, or PowerPoint.)*

5. রিবনে **"Mukti"** নামে একটি নতুন ট্যাব দেখতে পাবেন।
   *(A new "Mukti" tab appears in the ribbon.)*

---

### ⚠️ Windows নিরাপত্তা সতর্কতা (Windows Security Warning)

ইনস্টলার চালালে এই বার্তাটি দেখতে পারেন:

> **"Windows protected your PC"**

**এটি স্বাভাবিক এবং নিরাপদ।** মুক্তি একটি বিনামূল্যের ওপেন-সোর্স সফটওয়্যার — এর সম্পূর্ণ সোর্স কোড GitHub-এ পাবলিক। Windows এই বার্তাটি দেখায় কারণ মুক্তির একটি ব্যয়বহুল paid publisher certificate নেই।

This is normal and safe. Mukti is free, open-source software with its full source code publicly available. Windows shows this warning for any open-source tool without a paid publisher certificate.

**এই বার্তাটি দেখলে এই দুটি ধাপ অনুসরণ করুন:**

1. **"More info"** ক্লিক করুন *(click "More info")*
2. **"Run anyway"** ক্লিক করুন *(click "Run anyway")*

ইনস্টলার স্বাভাবিকভাবে খুলবে। *(The installer will open normally.)*

---

## ইনস্টল করুন — Mac (How to Install — Mac)

1. ডাউনলোড করা **`Mukti-x.x.xx.pkg`** ফাইলে **ডাবল-ক্লিক** করুন।
   *(Double-click the downloaded .pkg file.)*

2. **যদি Mac বলে "can't be opened":**
   ফাইলটিতে **Right-click** (বা Control+ক্লিক) করুন → **"Open"** ক্লিক করুন।
   এটি একবারই করতে হবে।
   *(If Mac says "can't be opened" — right-click the file and choose "Open". You only need to do this once.)*

3. ইনস্টলারের নির্দেশনা অনুসরণ করুন।
   *(Follow the installer steps.)*

4. ইনস্টলার সম্পূর্ণ হলে **Word, Excel বা PowerPoint** খুলুন।
   *(Open Word, Excel, or PowerPoint.)*

5. রিবনে **"Mukti"** বোতাম দেখতে পাবেন।
   *(The Mukti button appears in the ribbon.)*

> **Apple Silicon (M1/M2/M3):** Mac ইনস্টলারটি Intel এবং Apple Silicon উভয়ের জন্য তৈরি — আলাদা কোনো সংস্করণ নেই।
> *(The Mac installer works on both Intel and Apple Silicon Macs — no separate version needed.)*

---

## ব্যবহার করুন — ৩টি সহজ ধাপ (How to Use — 3 Simple Steps)

**ধাপ ১ — ফাইল খুলুন**

Word, Excel বা PowerPoint-এ আপনার পুরনো বিজয় বাংলা ফাইলটি খুলুন।
*(Open your old Bijoy Bengali file in Word, Excel, or PowerPoint.)*

---

**ধাপ ২ — Mukti প্যানেল খুলুন**

রিবনে **"Mukti"** ট্যাবে ক্লিক করুন। একটি প্যানেল খুলবে।
*(Click the "Mukti" tab in the ribbon. A panel opens on the side.)*

---

**ধাপ ৩ — স্ক্যান করুন এবং রূপান্তর করুন**

- **"Scan Document"** বোতামে ক্লিক করুন।
  মুক্তি আপনার ফাইলে পুরনো বিজয় বাংলা খুঁজে বের করবে।
  *(Click "Scan Document" — Mukti finds all legacy Bijoy Bengali text.)*

- রূপান্তরের একটি তালিকা দেখাবে। পরীক্ষা করুন।
  *(A list of text to be converted appears. Review it.)*

- **"Apply Conversion"** বোতামে ক্লিক করুন — সম্পন্ন!
  *(Click "Apply Conversion" — done!)*

> **ভুল হলে চিন্তা নেই!** Office-এর স্বাভাবিক **Ctrl+Z** (Undo) চাপলেই সব আগের মতো হয়ে যাবে। মুক্তির নিজস্ব **"Undo Conversion"** বোতামও আছে।
>
> *(Made a mistake? Press Ctrl+Z or click "Undo Conversion" to restore everything.)*

শুধু নির্বাচিত অংশ রূপান্তর করতে চাইলে **"Scan Selection"** ব্যবহার করুন।
*(To convert only selected text, use the "Scan Selection" button.)*

---

## মুক্তি কী কী রূপান্তর করে (What Mukti Converts)

| অ্যাপ্লিকেশন | কী রূপান্তরিত হয় |
|---|---|
| **Microsoft Word** | মূল পাঠ্য, টেবিল, হেডার, ফুটার, ফুটনোট, এন্ডনোট |
| **Microsoft Excel** | সেলের পাঠ্য (সূত্র/formula সেল বাদ দেওয়া হয়) |
| **Microsoft PowerPoint** | স্লাইডের পাঠ্য, স্পিকার নোট |

*(Word: body text, tables, headers, footers, footnotes, endnotes. Excel: cell text — formula cells are safely skipped. PowerPoint: slide text and speaker notes.)*

শুধু বিজয় বা সুতোন্নীএমজে ফন্টে লেখা পাঠ্য রূপান্তরিত হয়। অন্য ফন্ট অপরিবর্তিত থাকে।
*Only text written in Bijoy or SutonnyMJ fonts is converted. All other fonts and content are left untouched.*

---

## সিস্টেম চাহিদা (System Requirements)

| | Windows | Mac |
|---|---|---|
| **অপারেটিং সিস্টেম** | Windows 10 (version 1809) বা তার পরবর্তী | macOS 10.15 Catalina বা তার পরবর্তী |
| **Microsoft Office** | Office 2013 বা তার পরবর্তী | Office 2019 বা Microsoft 365 |
| **অতিরিক্ত প্রয়োজন** | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) (বিনামূল্যে, Microsoft প্রদত্ত) | কিছু লাগবে না |
| **ইন্টারনেট** | শুধু ডাউনলোড ও ইনস্টলের সময় | শুধু ডাউনলোড ও ইনস্টলের সময় |
| **হার্ডওয়্যার** | যেকোনো Windows 10 চালানো সক্ষম কম্পিউটার | যেকোনো macOS 10.15 চালানো সক্ষম Mac |

Windows-এ .NET 8 না থাকলে ইনস্টলার নিজেই ডাউনলোড পেজ খুলে দেবে।
*(On Windows, if .NET 8 is missing the installer opens the Microsoft download page automatically.)*

---

## গোপনীয়তা (Privacy)

**আপনার নথির কোনো তথ্য কখনো আপনার ডিভাইসের বাইরে যায় না।**

Your document content never leaves your device. Mukti runs entirely offline — no account required, no internet connection needed to convert documents, and no data is ever collected or transmitted.

---

## পূর্ববর্তী সংস্করণ থেকে আপগ্রেড (Upgrading from an Older Version)

v2.0.10 বা তার আগের সংস্করণ ইনস্টল থাকলে সেটি "সকল ব্যবহারকারীর জন্য" ইনস্টল করা ছিল (administrator হিসেবে)।
নতুন সংস্করণ শুধু আপনার জন্য ইনস্টল হয়। তাই নতুনটি ইনস্টলের আগে পুরনোটি সরিয়ে নিন।

*(v2.0.10 and earlier installed for all users with administrator rights. This version installs just for you. Uninstall the old Mukti from Windows Settings → Apps before running this installer — the installer will remind you if it finds an old version.)*

---

## সমস্যা হলে (Troubleshooting)

**Mukti ট্যাব দেখাচ্ছে না? (Mukti tab not showing?)**

1. Microsoft Word, Excel বা PowerPoint সম্পূর্ণ বন্ধ করুন। *(Close Office completely.)*
2. আবার খুলুন। *(Reopen it.)*
3. এখনো না দেখালে — Start Menu থেকে **"Repair Mukti (if it does not appear in Office)"** শর্টকাট চালান। *(Still missing? Run "Repair Mukti" from the Start Menu.)*

**অন্য কোনো সমস্যা? / Other issues?**

[GitHub Issues](https://github.com/GRU-953/Mukti/issues)-এ জানান। বাংলায় লিখলেও চলবে।
*(Report at GitHub Issues — you can write in Bengali.)*

---

<div align="center">

[![MIT License](https://img.shields.io/badge/License-MIT-E0990F?style=flat-square)](LICENSE)
&nbsp;&nbsp;
[![GRU-953](https://img.shields.io/badge/author-GRU--953-26336E?style=flat-square)](https://github.com/GRU-953)
&nbsp;&nbsp;
[![GitHub Stars](https://img.shields.io/github/stars/GRU-953/Mukti?style=flat-square&color=14A88A)](https://github.com/GRU-953/Mukti/stargazers)
&nbsp;&nbsp;
[![Latest Release](https://img.shields.io/github/v/release/GRU-953/Mukti?style=flat-square&color=E15B3C&label=latest)](https://github.com/GRU-953/Mukti/releases/latest)

**Aninda Sundar Howlader (GRU-953)** · [aninda.sh15@gmail.com](mailto:aninda.sh15@gmail.com)

*মুক্তি মানে স্বাধীনতা। Mukti means freedom.*

*Simple technology. For everyone.*

</div>