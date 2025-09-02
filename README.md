# WiseTorrent

## What is a BitTorrent client?
BitTorrent is a file-sharing protocol that uses a peer-to-peer (P2P) network to distribute large files over the internet. It functions by splitting up files into smaller pieces, which each peer in the swarm (peer network) shares with other peers becoming a temporary source of the file by both uploading and downloading pieces. A BitTorrent client is a tool to download a torrent file, it takes in a .TORRENT file from the user that contains concise metadata for their desired file, and the client handles the locating of peers, and the connection and communication with the swarm, in line with the BitTorrent protocol, in order to produce the user their desired file.

## Why use a BitTorrent client?
Torrenting files using a BitTorrent client provides many benefits over the regular client-server download, in which a downloader receives the file over one connection to a server containing the file. The P2P nature of the BitTorrent protocol makes the process decentralised, granting torrenters privacy over their downloads. This is different to the client-server download process, where there is no anonymity and a users download habits leave a trace. The distributed nature of the torrenting process also often allows for higher download speeds due to the reduced individual burden on just one source, the more popular a torrent file, the even faster it can get.

## About WiseTorrent
WiseTorrent is a BitTorrent client designed by Alex Chen, Joyce Ching-Xuan Yap, and Zane Tonitto, for Application Development with .NET (31291). The app is designed using C# .NET, with a Blazor WebView imbedded within a WPF standalone application.
