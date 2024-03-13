### 1. 
Анализ символов программы

`x security_bypass!*` - все функции модуля `security_bypass`  

```
0:000> x security_bypass!*
00007ff7`86861091 security_bypass!security_fail (security_fail)
00007ff7`86861004 security_bypass!main (main)
00007ff7`86866010 security_bypass!_imp_ExitProcess = <no type information>
00007ff7`86866018 security_bypass!KERNEL32_NULL_THUNK_DATA = <no type information>
00007ff7`86862117 security_bypass!WriteFile (WriteFile)
00007ff7`86866000 security_bypass!_imp_GetStdHandle = <no type information>
00007ff7`86861000 security_bypass!security_cookie (security_cookie)
00007ff7`8686210b security_bypass!ExitProcess (ExitProcess)
00007ff7`86862111 security_bypass!GetStdHandle (GetStdHandle)
00007ff7`86866070 security_bypass!_IMPORT_DESCRIPTOR_KERNEL32 = <no type information>
00007ff7`86866008 security_bypass!_imp_WriteFile = <no type information>
00007ff7`86866084 security_bypass!_NULL_IMPORT_DESCRIPTOR = <no type information>
```

### 2. 
Видим main, ставим туда breakpoint

```
bp security_bypass!main
```

### 3. 
Перезапускаем и ловим брейкпоинт

```
0:000> g
Breakpoint 0 hit
security_bypass!main:
00007ff7`86861004 813df2ffffffbebafeca cmp dword ptr [security_bypass!security_cookie (00007ff7`86861000)],0CAFEBABEh ds:00007ff7`86861000=deadbeef
```
Запомним этот вывод, но для понятности можно сделать unassemble

```
0:000> u
security_bypass!main:
00007ff7`86861004 813df2ffffffbebafeca cmp dword ptr [security_bypass!security_cookie (00007ff7`86861000)],0CAFEBABEh
00007ff7`8686100e 7405            je      security_bypass!main+0x11 (00007ff7`86861015)
00007ff7`86861010 e87c000000      call    security_bypass!security_fail (00007ff7`86861091)
00007ff7`86861015 488d35e43f0000  lea     rsi,[security_bypass!WriteFile <PERF> (security_bypass+0x5000) (00007ff7`86865000)]
00007ff7`8686101c 488d3ddd3f0000  lea     rdi,[security_bypass!WriteFile <PERF> (security_bypass+0x5000) (00007ff7`86865000)]
00007ff7`86861023 fc              cld
00007ff7`86861024 b921000000      mov     ecx,21h
00007ff7`86861029 ac              lods    byte ptr [rsi]
```

Видим, что логика там такая: сделай `cmp`, затем `je` на main else `call` main.
`cmp` - это команда сравнения. `je` - выполняет инструкцию, если `cmp` дала `true`. В противном случае выполняется следующая инструкция (у нас это `call`). Названия методов тоже понятные.  
  
Собирая всё в кучу осознаём, что сначала происходит какое-то сравнение, если оно удачное, то вызывается `main`, иначе происходит `security_fail`. По имени последней можно догадаться, что проверка непростая.  

В команде `cmp` видим, что 
```
[security_bypass!security_cookie (00007ff7c7ab1000)]
```
указывает на использование символа `security_bypass!security_cookie` по адресу `00007ff7c7ab1000`.  
  
Возвращаемся к выводу после остановки на брейкпоинте. Для нас любезно уже преобразовали значение, с которым сравнивают.  
```
... ,0CAFEBABEh ds:00007ff7`86861000=deadbeef
```

### 4. 
Осталось только подставить правильное значение по правильному адресу:   
```
ed 7ff786861000 0CAFEBABEh
или
ed security_bypass!security_cookie 0CAFEBABEh
```

### 5.
После продолжения выполнения, в окне консоли появится результат.