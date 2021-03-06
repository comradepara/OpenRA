CSC     = gmcs
CSFLAGS  = -nologo -warn:4 -debug:+ -debug:full -optimize- -codepage:utf8 -unsafe
DEFINE  = DEBUG;TRACE
PROGRAMS	=fileformats gl game ra cnc aftermath ra_ng server seqed

COMMON_LIBS	= System.dll System.Core.dll System.Drawing.dll System.Xml.dll

fileformats_SRCS	=	$(shell find OpenRA.FileFormats/ -iname '*.cs')
fileformats_TARGET	=	OpenRA.FileFormats.dll
fileformats_KIND	=	library
fileformats_LIBS	=	$(COMMON_LIBS) thirdparty/Tao/Tao.Sdl.dll

gl_SRCS				= $(shell find OpenRA.Gl/ -iname '*.cs')
gl_TARGET			= OpenRA.Gl.dll
gl_KIND				= library
gl_DEPS				= $(fileformats_TARGET) $(game_TARGET)
gl_LIBS				= $(COMMON_LIBS) System.Windows.Forms.dll \
						thirdparty/Tao/Tao.Cg.dll thirdparty/Tao/Tao.OpenGl.dll thirdparty/Tao/Tao.Sdl.dll \
						$(gl_DEPS) $(game_TARGET)

game_SRCS			=	$(shell find OpenRA.Game/ -iname '*.cs')
game_TARGET			= OpenRA.Game.exe
game_KIND			= winexe
game_DEPS			= $(fileformats_TARGET) 
game_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(game_DEPS) \
						thirdparty/Tao/Tao.OpenAl.dll thirdparty/Tao/Tao.FreeType.dll
game_FLAGS			= -win32icon:OpenRA.Game/OpenRA.ico

ra_SRCS				=	$(shell find OpenRA.Mods.RA/ -iname '*.cs')
ra_TARGET			=	mods/ra/OpenRA.Mods.RA.dll
ra_KIND				=	library
ra_DEPS				= $(fileformats_TARGET) $(game_TARGET)
ra_LIBS				= $(COMMON_LIBS) $(ra_DEPS)

cnc_SRCS			=	$(shell find OpenRA.Mods.Cnc/ -iname '*.cs')
cnc_TARGET			=	mods/cnc/OpenRA.Mods.Cnc.dll
cnc_KIND			=	library
cnc_DEPS			= $(fileformats_TARGET) $(game_TARGET) $(ra_TARGET)
cnc_LIBS			= $(COMMON_LIBS) $(cnc_DEPS)

aftermath_SRCS		=	$(shell find OpenRA.Mods.Aftermath/ -iname '*.cs')
aftermath_TARGET	=	mods/aftermath/OpenRA.Mods.Aftermath.dll
aftermath_KIND		=	library
aftermath_DEPS		= $(fileformats_TARGET) $(game_TARGET) $(ra_TARGET)
aftermath_LIBS		= $(COMMON_LIBS) $(aftermath_DEPS)

ra_ng_SRCS		=	$(shell find OpenRA.Mods.RA-NG/ -iname '*.cs')
ra_ng_TARGET		=	mods/ra-ng/OpenRA.Mods.RA_NG.dll
ra_ng_KIND		=	library
ra_ng_DEPS		= $(ra_TARGET) $(fileformats_TARGET) $(game_TARGET) $(ra_TARGET)
ra_ng_LIBS		= $(COMMON_LIBS) $(ra_ng_DEPS)

seqed_SRCS			= $(shell find SequenceEditor/ -iname '*.cs')
seqed_TARGET		= SequenceEditor.exe
seqed_KIND			= winexe
seqed_DEPS			= $(fileformats_TARGET)
seqed_LIBS			= $(COMMON_LIBS) System.Windows.Forms.dll $(seqed_DEPS)

# -platform:x86



define BUILD_ASSEMBLY

$$($(1)_TARGET): $$($(1)_SRCS) Makefile $$($(1)_DEPS)
	@echo CSC $$(@)
	@$(CSC) $$($(1)_LIBS:%=-r:%) \
		-out:$$(@) $(CSFLAGS) $$($(1)_FLAGS) \
		-define:"$(DEFINE)" \
		-t:"$$($(1)_KIND)" \
		$$($(1)_SRCS)
endef


$(foreach prog,$(PROGRAMS),$(eval $(call BUILD_ASSEMBLY,$(prog))))


.SUFFIXES:
.PHONY: clean all default mods server seqed


clean: 
	@-rm *.exe *.dll *.mdb mods/**/*.dll mods/**/*.mdb

mods: $(ra_TARGET) $(cnc_TARGET) $(aftermath_TARGET) $(ra_ng_TARGET)
seqed: $(seqed_TARGET)
all: $(fileformats_TARGET) $(gl_TARGET) $(game_TARGET) $(ra_TARGET) $(cnc_TARGET) $(aftermath_TARGET) $(ra_ng_TARGET) $(seqed_TARGET)

dist-osx:
	packaging/osx/package.sh

.DEFAULT: all
