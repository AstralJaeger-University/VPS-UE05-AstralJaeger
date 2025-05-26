VPS5 - Exercise 5
=================

Name: Hillebrand Felix

Effort in hours: ___

## 1. Stock Data Visualization

### Task 1.a

Der ``TaskClient`` implementiert die Asynchronität mithilfe der ``Task Parallel Library (TPL)`` und setzt dabei auf und ``Task.Factory.StartNew`` und ``ContinueWith``.
Für jeden Aktientitel wird zuerst ein Task zum Abrufen der Daten (``RetrieveStockData``) gestartet. 
An diesen Task wird mittels ``ContinueWith`` ein weiterer Task angehängt, der nach Abschluss des ersten Tasks die erhaltenen Daten weiterverarbeitet, um die Zeitreihen und den Trend zu berechnen. 
Schließlich wird ``Task.Factory.ContinueWhenAll`` genutzt, um zu warten, bis alle diese verketteten Aufgaben abgeschlossen sind, woraufhin die Ergebnisse aggregiert und das ``RequestCompleted``-Ereignis ausgelöst wird.

### Task 1.b

Der `AsyncClient` verwendet das `async` und `await` Muster von C# für die asynchrone Verarbeitung. 
Die `RequestAsync`-Methode startet für jeden Aktientitel eine asynchrone Operation `ProcessStockDataAsync`. 
Innerhalb von `ProcessStockDataAsync` wird das Abrufen der Aktiendaten sowie die Berechnung der Zeitreihen und des Trends jeweils in eigenen `Task.Run`-Aufrufen gekapselt und mit `await` erwartet.
`Task.WhenAll` wird verwendet, um auf den Abschluss aller parallelen Aktienverarbeitungsaufgaben zu warten, bevor die Ergebnisse gesammelt und das `RequestCompleted`-Ereignis ausgelöst wird. 

## 2. Parallel Wator

### Task 2.a

Grundidee: Das Gitter wird in mehrere Bereiche aufgeteilt, die gleichzeitig bearbeitet werden können.
Ansatz:

Gitter in Partitionen unterteilen (z.B. basierend auf CPU-Kernen)
Jede Partition parallel verarbeiten mit Parallel.ForEach
Zufällige Verarbeitungsreihenfolge innerhalb der Partitionen verwenden
Globales Lock für Zugriffe auf geteilte Daten (Gitter, Tiere)
Zwei-Phasen-Verarbeitung: erst Entscheidungen treffen, dann ausführen

**Vorteile**:

- Nutzt mehrere CPU-Kerne
- Randomisierung vermeidet systematische Fehler

**Nachteile** :

- Lock kann Engpass werden
- Thread-Overhead
- Ungleichmäßige Lastverteilung möglich

### Task 2.b

| Method              | Mean    | Error    | StdDev   |
|-------------------- |--------:|---------:|---------:|
| OriginalPerformance | 1.567 s | 0.0251 s | 0.0269 s |
| ParallelPerformance | 1.618 s | 0.0393 s | 0.0436 s |

Speedup (oder eher slowdown): $s=\frac{t_s}{t_p}=\frac{1.567s}{1.618s}=0.968=98.2\%$.
Es lässt sich ein slowdown von $3.2\%$ feststellen.
Dies liegt wahrscheinlich daran, dass die Implementierung nur sehr grob und 
schnell umgesetzt wurde und keine besondere Aufmerksamkeit der Optimierung sondern lediglich der Parallelisierung gegeben wurde.