import threading
import socket
import ssl
import shlex
import os
import time
from prettytable import PrettyTable
from colorama import init
from termcolor import colored
init(convert=True)
context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
context.load_cert_chain('ssl/certificate.crt', 'ssl/privateKey.key')
connList = []
activeConn = 0
optionsDict = {}
optionsList = []
moduleSrc = ""
cont = True
sessionsTable = []
optionsTable = []
currentModule = ""
def ReadLoop(conn):
    global activeConn
    global connList
    global cont
    while True:
        try:
            if len(connList) > 0:
                dat = connList[activeConn].recv(1024)
                dat = dat.decode('utf-8')
                dat = dat.replace("\r\n","\n")
                if 'SIGNAL-MODULE-FINISHED' in dat:
                    print("\n"+dat.replace('SIGNAL-MODULE-FINISHED',''))
                    print(colored("[*] Module Finished.","cyan"))
                else:
                    if dat[1] == "*":
                        print(colored(dat,"cyan"))
                    elif dat[1] == "+":
                        print(colored(dat,"green"))
                    elif dat[1] == "-":
                        print(colored(dat,"red"))
                    else:
                        print(dat)
            else:
                pass
        except ConnectionResetError:
            for index, connection in enumerate(connList):
                try:
                    connection.send(b"//SIGNAL-CHECK-ALIVE")
                except socket.error:
                    print(colored("\n[-] Client "+str(index)+" is disconnected.","red"))
                    connList.remove(connection)
                    print(colored("[*] Removed disconnected client from sessions list.","cyan"))
                    if len(connList) == 0:
                        print(colored("[-] No active connections. Quitting.","red"))
                        cont = False
                    else:
                        activeConn = 0
                        print(colored("[*] Set active connection to 0.","cyan"))
            #pdb.set_trace()
def WriteLoop(conn):
    global optionsDict
    global activeConn
    global connList
    global cont
    while True:
        inp = input("SilverFlame"+currentModule+">")
        try:
            ProcessInput(inp, connList[activeConn])
        except (ConnectionResetError, IndexError):
            print(colored("[-] Client: "+str(activeConn)+" is disconnected.","red"))
            if len(connList) > 0:
                print(colored("[*] Set active connection to 0.","cyan"))
                activeConn = 0

def UseFunc(inn, conn):
    #Load file & metadata
    global optionsDict
    global optionsList
    global moduleSrc
    global optionsTable
    global currentModule
    optionsTable = []
    tokens=shlex.split(inn)
    filepath = "modules/"+tokens[1]
    moduleSrc = ""
    try:
        with open(filepath, "r") as f:
            moduleSrc = f.read()
            #print(moduleSrc) #debugging
            print(colored('[*] Using module /'+filepath, 'cyan'))
            filepath=filepath.replace("\\","/")
            currentModule = "/"+filepath
    except OSError:
        print(colored("[-] That module does not exist :(","red"))
        return 1
    lines=moduleSrc.splitlines()
    language = lines[0]
    if language == "//C#":
        #process acordingly
        options = lines[5][2:].split(",")
        for option in options:
            optionsDict[option] = ""
            optionsList.append(option)
    elif language == "//IL-DATA":
        options = lines[4][2:].split(",")
        for option in options:
            optionsDict[option] = ""
            optionsList.append(option)
    else:
        options = lines[1][2:].split(",")
        for option in options:
            optionsDict[option] = ""
            optionsList.append(option)
    #print(optionsDict)
    #code.interact(local=locals())
def RunFunc(inn, conn):
    global optionsList
    global optionsDict
    global moduleSrc
    for option in optionsList:
        print(option,optionsDict[option])
        moduleSrc=moduleSrc.replace(option,optionsDict[option])
    payload = "//MODULE-START\n" + moduleSrc + "\n" + "//MODULE-END\n\r\n"
    conn.sendall(payload.encode('utf-8'))
def SetFunc(inn, conn):
    global optionsDict
    global optionsList
    global optionsTable
    tokens=shlex.split(inn)
    var = tokens[1]
    val = tokens[2]
    if var in optionsList:
        optionsDict[var] = val
        tableObj = [var, val]
        optionsTable.append(tableObj)
        print(colored("[*] Set option "+var+" to "+val,"cyan"))
    else:
        print("Option "+var+" not available in module.")
def ShowFunc(inn, conn):
    #show data abt current module
    tokens = inn.split(" ")
    if tokens[1] == "options":
        t = PrettyTable(['Option', 'Value'])
        for optionx in optionsTable:
            t.add_row(optionx)
        print("===SET OPTIONS===")
        print(t)
        print("===UNSET OPTIONS===")
        for optionx in optionsList:
            print(optionx)
def PromptFunc(inn, conn):
    pass
def InteractFunc(inn, conn):
    global activeConn
    tokens = inn.split(" ")
    if int(tokens[1]) < len(connList):
        activeConn = int(tokens[1])
        print(colored("[*] Set active connection to "+tokens[1],"cyan"))
    else:
        print(colored("[-] Session #"+tokens[1]+" is not connected.","red"))
def ExitFunc(inn,conn):
    global cont
    cont = False
    #_thread.interrupt_main()
    #sys.exit(0)
def PyExec(inn,conn):
    eval(inn[5:])
def SessionsFunc(inn, conn):
    t = PrettyTable(['Session #', 'IP Adress'])
    for row in sessionsTable:
        t.add_row(row)
    print(t)
def ListFunc(inn,conn):
    FilesAndFolders = os.listdir("modules/"+inn[5:])
    for item in FilesAndFolders:
        if "." in item:
            print(colored(item,"green"))
        else:
            print(colored(item,"cyan"))
def ProcessInput(inn, conn):
    key=inn.split(" ")[0]
    switcher = {
        "use":UseFunc,
        "run":RunFunc,
        "set":SetFunc,
        "show":ShowFunc,
        "interact":InteractFunc,
        "exit":ExitFunc,
        "exec":PyExec,
        "sessions":SessionsFunc,
        "list":ListFunc,
        "":PromptFunc,
    }
    func = switcher.get(key)
    if func == None:
        if inn.startswith("!"):
            conn.sendall((inn+"\r\n").encode('utf-8'))
            return 0
        else:
            print(colored("[-] That command does not exist :(","red"))
            return 1
    return func(inn.replace("\\","\\\\"), conn)
threads = []
with socket.socket(socket.AF_INET, socket.SOCK_STREAM, 0) as sock:
    sock.bind(('127.0.0.1', 443))
    sock.listen(5)
    with context.wrap_socket(sock, server_side=True) as ssock:
            conn, addr = ssock.accept()
            print(colored("\n[+] New connection from "+str(addr)+" as session #"+str(len(connList)),"green"))
            tableObj = [len(connList), addr[0]]
            sessionsTable.append(tableObj)
            connList.append(conn)
            readThread = threading.Thread(target=ReadLoop, args=(conn, ))
            readThread.setDaemon(True)
            threads.append(readThread)
            readThread.start()
            writeThread = threading.Thread(target=WriteLoop, args=(conn, ))
            writeThread.setDaemon(True)
            threads.append(writeThread)
            writeThread.start()
            ssock.setblocking(0)
            while True:
                try:
                    conn, addr = ssock.accept()
                    print(colored("\n[+] New connection from "+str(addr)+" as session #"+str(len(connList)),"green"))
                    tableObj = [len(connList), addr[0]]
                    sessionsTable.append(tableObj)
                    connList.append(conn)
                except BlockingIOError:
                    pass
                if cont==False:
                    break #try to exit cleanly
                time.sleep(0.1)
