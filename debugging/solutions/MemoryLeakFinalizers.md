### 1. 
Делаем `!dumpheap -stat`

### 2.
Продолжаем выполнение и снова делаем `!dumpheap -stat`.
Там не особо что-то изменилось, кроме строчки Freee.

### 3.
Смотрим очередь объектов на утилизацию !finalizequeue
```
0:009> !finalizequeue
...
Ready for finalization 4269 objects (000001e6a1c4cd78->000001e6a1c552e0)
Statistics for all finalizable objects (including all objects ready for finalization):
              MT    Count    TotalSize Class Name
00007fff45764078        1           32 Microsoft.Win32.SafeHandles.SafePEFileHandle
00007fff45763b48        1           64 System.Threading.ReaderWriterLock
00007ffeec115b40     4272       102528 MemoryLeakFinalizers.MyDataProcessor
Total 4274 objects
```

### 4.
Также можно посмотреть, чем заняты потоки. Через !runaway и !clrstack.
Там будет видно, что подобное
```
0:007> ~6e!clrstack
OS Thread Id: 0x3c08 (6)
        Child SP               IP Call Site
0000006C8FCFF7A8 00007ffad2801140 MemoryLeakFinalizers.MyDataProcessor.Finalize()
0000006C8FCFF7B0 00007ffad28010f2 MemoryLeakFinalizers.MyDataProcessor.Finalize()
0000006C8FCFFD60 00007ffb322feb56 [DebuggerU2MCatchHandlerFrame: 0000006c8fcffd60]
```

### 5.
Вывод:

* Ссылок на объекты не остаётся
* GC считает их области в памяти as Free
* Количество живых объектов, связанных с MemoryLeakFinalizers.MyDataProcessor, не увеличивается 

Тем не менее, объекты зависают в промежуточном состоянии и GC кладёт их в очередь. 
Если посмотреть help finalizequeue, там написано подробнее, что это такое.
Память утекает потому, что куча сжимается лишь после того, как её очистят.

В коде это выглядит как метод с перфиксом ~ (финализатор), который позволяет объекту 
прям перед приходом GC дословно: попытаться освободить ресурсы и выполнить очистку самостоятельно. 
Но в нём while(true) :)